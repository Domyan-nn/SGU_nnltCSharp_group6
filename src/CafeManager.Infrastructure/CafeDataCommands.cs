using System.Data;
using System.Security.Cryptography;
using System.Text.Json;
using CafeManager.Core;
using Microsoft.EntityFrameworkCore;

namespace CafeManager.Infrastructure;

// Transactional data primitives. Authorization and complete UC state transitions belong to future application handlers.
// Use a fresh DI scope/context for each command; discard the context after a failed command.
public sealed class CafeDataCommands(CafeDbContext db) : ICafeDataCommands
{
    private Task LockAsync(string resource, CancellationToken ct) =>
        db.Database.ExecuteSqlInterpolatedAsync($@"
DECLARE @r int;
EXEC @r = sys.sp_getapplock @Resource={resource}, @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=15000;
IF @r < 0 THROW 51000, 'Cannot acquire operation lock', 1;", ct);

    public async Task<long> AcceptRoundAsync(long roundId, DateTime utcNow, CancellationToken ct = default)
    {
        if (utcNow.Kind != DateTimeKind.Utc) throw new ArgumentException("UTC required.", nameof(utcNow));
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        await LockAsync($"accept-round:{roundId}", ct);
        var round = await db.OrderRound.SingleAsync(x => x.RoundId == roundId, ct);
        var order = await db.SalesOrder.SingleAsync(x => x.OrderId == round.OrderId, ct);
        if (round.Status == "Accepted")
        {
            var existing = await db.PreparationTicket.SingleAsync(x => x.RoundId == roundId, ct);
            await tx.CommitAsync(ct);
            return existing.TicketId;
        }
        if (round.Status != "Draft" || order.Status is not ("Pending" or "Serving"))
            throw new InvalidOperationException("Round/order cannot be accepted.");
        var lines = await db.OrderLine.Where(x => x.RoundId == roundId).OrderBy(x => x.LineId).ToListAsync(ct);
        if (lines.Count == 0) throw new InvalidOperationException("Empty round.");
        var ticket = new PreparationTicket { RoundId = roundId, Status = "Waiting", CreatedAt = utcNow };
        round.Status = "Accepted"; round.AcceptedAt = utcNow; order.Status = "Serving";
        db.Add(ticket);
        await db.SaveChangesAsync(ct);

        var requirements = new Dictionary<long, decimal>();
        var unitIngredients = new List<PreparationIngredient>();
        foreach (var line in lines)
        {
            var variant = await db.ProductVariant.SingleAsync(x => x.VariantId == line.VariantId, ct);
            var product = await db.Product.SingleAsync(x => x.ProductId == variant.ProductId, ct);
            if (!variant.IsActive || !product.IsActive || variant.VariantType != "Drink")
                throw new InvalidOperationException("Unavailable drink.");
            var recipe = await CurrentRecipeAsync(line.VariantId, utcNow, ct);
            var quantities = await RecipeQuantitiesAsync(recipe.RecipeVersionId, ct);
            var addons = await db.OrderLineAddon.Where(x => x.LineId == line.LineId).ToListAsync(ct);
            foreach (var addon in addons)
            {
                var addonVariant = await db.ProductVariant.SingleAsync(x => x.VariantId == addon.AddonVariantId, ct);
                var addonProduct = await db.Product.SingleAsync(x => x.ProductId == addonVariant.ProductId, ct);
                if (addonVariant.VariantType != "Addon" || !addonVariant.IsActive || !addonProduct.IsActive)
                    throw new InvalidOperationException("Unavailable addon.");
                var addonRecipe = await CurrentRecipeAsync(addon.AddonVariantId, utcNow, ct);
                foreach (var (ingredient, quantity) in await RecipeQuantitiesAsync(addonRecipe.RecipeVersionId, ct))
                    quantities[ingredient] = quantities.GetValueOrDefault(ingredient) + quantity * addon.Quantity;
            }
            for (var number = 1; number <= line.Quantity; number++)
            {
                var unit = new PreparationUnit { TicketId = ticket.TicketId, LineId = line.LineId,
                    UnitNumber = number, Status = "Waiting", RecipeVersionId = recipe.RecipeVersionId };
                db.Add(unit);
                await db.SaveChangesAsync(ct);
                foreach (var (ingredient, quantity) in quantities)
                {
                    requirements[ingredient] = requirements.GetValueOrDefault(ingredient) + quantity;
                    unitIngredients.Add(new PreparationIngredient { UnitId = unit.UnitId, IngredientId = ingredient,
                        QuantitySnapshot = quantity, QuantityReserved = quantity });
                }
            }
        }
        foreach (var (ingredientId, quantity) in requirements.OrderBy(x => x.Key))
        {
            var affected = await db.StockBalance.Where(x => x.IngredientId == ingredientId &&
                x.QuantityOnHand - x.QuantityReserved >= quantity)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.QuantityReserved, x => x.QuantityReserved + quantity), ct);
            if (affected != 1) throw new InvalidOperationException($"Insufficient available stock: {ingredientId}");
        }
        db.AddRange(unitIngredients);
        foreach (var item in unitIngredients)
            db.Add(Movement(item.IngredientId, item.QuantityReserved, "Reserve", "PreparationUnit", item.UnitId, utcNow));
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return ticket.TicketId;
    }

    private async Task<RecipeVersion> CurrentRecipeAsync(long variantId, DateTime now, CancellationToken ct) =>
        await db.RecipeVersion.SingleOrDefaultAsync(x => x.VariantId == variantId &&
            x.EffectiveFrom <= now && (x.EffectiveTo == null || now < x.EffectiveTo), ct)
        ?? throw new InvalidOperationException($"No effective recipe: {variantId}");

    private async Task<Dictionary<long, decimal>> RecipeQuantitiesAsync(long recipeId, CancellationToken ct)
    {
        var quantities = await db.RecipeDetail.Where(x => x.RecipeVersionId == recipeId)
            .ToDictionaryAsync(x => x.IngredientId, x => x.QuantityRequired, ct);
        if (quantities.Count == 0) throw new InvalidOperationException("Empty recipe.");
        return quantities;
    }

    public async Task StartUnitAsync(long unitId, DateTime utcNow, CancellationToken ct = default)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        await LockAsync($"start-unit:{unitId}", ct);
        var unit = await db.PreparationUnit.SingleAsync(x => x.UnitId == unitId, ct);
        if (unit.Status != "Waiting") throw new InvalidOperationException("Unit must be waiting.");
        var ingredients = await db.PreparationIngredient.Where(x => x.UnitId == unitId).OrderBy(x => x.IngredientId).ToListAsync(ct);
        if (ingredients.Count == 0) throw new InvalidOperationException("Missing reservation.");
        foreach (var item in ingredients)
        {
            if (item.QuantityReserved != item.QuantitySnapshot) throw new InvalidOperationException("Reservation mismatch.");
            var amount = item.QuantityReserved;
            var changed = await db.StockBalance.Where(x => x.IngredientId == item.IngredientId && x.QuantityReserved >= amount && x.QuantityOnHand >= amount)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.QuantityReserved, x => x.QuantityReserved - amount)
                    .SetProperty(x => x.QuantityOnHand, x => x.QuantityOnHand - amount), ct);
            if (changed != 1) throw new InvalidOperationException("Stock mismatch.");
            item.QuantityReserved = 0;
            db.Add(Movement(item.IngredientId, amount, "Consume", "PreparationUnit", unitId, utcNow));
        }
        unit.Status = "Preparing";
        db.Add(new PreparationAttempt { UnitId = unitId, AttemptNo = 1, Outcome = "Started", CreatedAt = utcNow });
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    private static StockMovement Movement(long ingredientId, decimal quantity, string type, string source, long id, DateTime now) =>
        new() { IngredientId = ingredientId, Quantity = quantity, MovementType = type, SourceType = source,
            SourceId = id, MovementCode = Guid.NewGuid().ToString("N"), CreatedAt = now };

    private static string Hash(object payload) => Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(payload)));

    private async Task<IdempotencyRecord> BeginRequestAsync(Guid id, string hash, CancellationToken ct)
    {
        if (id == Guid.Empty) throw new ArgumentException("RequestId must not be empty.");
        await LockAsync($"request:{id:D}", ct);
        var record = await db.IdempotencyRecord.FindAsync([id], ct);
        if (record != null)
        {
            if (record.RequestHash != hash) throw new InvalidOperationException("RequestId reused with different content.");
            return record;
        }
        record = new IdempotencyRecord { RequestId = id, RequestHash = hash, CreatedAt = DateTime.UtcNow };
        db.Add(record);
        await db.SaveChangesAsync(ct);
        return record;
    }

    private async Task RequireOpenShiftAsync(long shiftId, long staffId, CancellationToken ct)
    {
        // A future close-shift handler must take the same SQL row lock before reconciliation/closing.
        var shift = await db.Shift.FromSqlInterpolated($"SELECT * FROM dbo.Shift WITH (UPDLOCK,HOLDLOCK) WHERE ShiftId={shiftId}")
            .SingleAsync(ct);
        if (shift.ClosedAt != null || !await db.StaffAccount.AnyAsync(x => x.StaffId == staffId && x.IsActive, ct))
            throw new InvalidOperationException("Closed shift or inactive staff.");
    }

    public async Task<long> RecordCashPaymentAsync(Guid requestId, long orderId, long shiftId, long staffId, decimal amount, CancellationToken ct = default)
    {
        if (amount <= 0 || decimal.Round(amount, 2) != amount) throw new ArgumentOutOfRangeException(nameof(amount));
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var request = await BeginRequestAsync(requestId, Hash(new { Operation = "CashPayment", orderId, shiftId, staffId,
            Amount = amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) }), ct);
        if (request.ResultReference != null)
        {
            var result = long.Parse(request.ResultReference, System.Globalization.CultureInfo.InvariantCulture);
            await tx.CommitAsync(ct); return result;
        }
        await RequireOpenShiftAsync(shiftId, staffId, ct);
        if (!await db.SalesOrder.AnyAsync(x => x.OrderId == orderId && (x.Status == "Serving" || x.Status == "Settling"), ct))
            throw new InvalidOperationException("Order is not accepted for payment.");
        // This primitive records a verified cash receipt. Bill calculation/overpayment policy is a future UC08 handler.
        var payment = new Payment { PaymentCode = Guid.NewGuid().ToString("N"), OrderId = orderId, ShiftId = shiftId,
            Amount = amount, Method = "Cash", Status = "Succeeded", PaidAt = DateTime.UtcNow, RequestId = requestId, CreatedBy = staffId };
        db.Add(payment); await db.SaveChangesAsync(ct);
        request.ResultReference = payment.PaymentId.ToString(System.Globalization.CultureInfo.InvariantCulture);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return payment.PaymentId;
    }

    public async Task<long> RecordCashRefundAsync(Guid requestId, long refundId, long shiftId, long staffId, CancellationToken ct = default)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var request = await BeginRequestAsync(requestId, Hash(new { Operation = "CashRefund", refundId, shiftId, staffId }), ct);
        if (request.ResultReference != null)
        {
            var result = long.Parse(request.ResultReference, System.Globalization.CultureInfo.InvariantCulture);
            await tx.CommitAsync(ct); return result;
        }
        await RequireOpenShiftAsync(shiftId, staffId, ct);
        var refund = await db.RefundRequest.SingleAsync(x => x.RefundId == refundId, ct);
        var payment = await db.Payment.FromSqlInterpolated($"SELECT * FROM dbo.Payment WITH (UPDLOCK,HOLDLOCK) WHERE PaymentId={refund.PaymentId}")
            .SingleAsync(ct);
        if (refund.Status != "Approved" || payment.Status != "Succeeded")
            throw new InvalidOperationException("Refund must be approved against successful payment.");
        var used = await (from rt in db.RefundTransaction join rr in db.RefundRequest on rt.RefundId equals rr.RefundId
            where rr.PaymentId == payment.PaymentId && rt.Status == "Succeeded" select (decimal?)rt.Amount).SumAsync(ct) ?? 0;
        if (used + refund.Amount > payment.Amount || await db.RefundTransaction.AnyAsync(x => x.RefundId == refundId && x.Status != "Failed", ct))
            throw new InvalidOperationException("Refund exceeds allowance or already executed/pending.");
        var item = new RefundTransaction { RefundId = refundId, ShiftId = shiftId, Amount = refund.Amount,
            Method = "Cash", Status = "Succeeded", RequestId = requestId, ExecutedAt = DateTime.UtcNow, CreatedBy = staffId };
        db.Add(item); await db.SaveChangesAsync(ct);
        request.ResultReference = item.RefundTransactionId.ToString(System.Globalization.CultureInfo.InvariantCulture);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return item.RefundTransactionId;
    }

    public async Task<ShiftReconciliation> ReconcileShiftAsync(long shiftId, CancellationToken ct = default)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var shift = await db.Shift.AsNoTracking().SingleAsync(x => x.ShiftId == shiftId, ct);
        var payments = await db.Payment.Where(x => x.ShiftId == shiftId && x.Status == "Succeeded" && x.Method == "Cash")
            .SumAsync(x => (decimal?)x.Amount, ct) ?? 0;
        var refunds = await db.RefundTransaction.Where(x => x.ShiftId == shiftId && x.Status == "Succeeded" && x.Method == "Cash")
            .SumAsync(x => (decimal?)x.Amount, ct) ?? 0;
        var entries = await db.ShiftCashEntry.Where(x => x.ShiftId == shiftId && x.Status == "Posted").ToListAsync(ct);
        var result = new ShiftReconciliation(shift.OpeningCash, payments, refunds,
            entries.Where(x => x.EntryType == "OtherIn").Sum(x => x.Amount),
            entries.Where(x => x.EntryType == "OtherOut").Sum(x => x.Amount), shift.CountedCash);
        await tx.CommitAsync(ct); return result;
    }
}

