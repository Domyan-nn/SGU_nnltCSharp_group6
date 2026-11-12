using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using CafeManager.Core;
using Microsoft.EntityFrameworkCore;

namespace CafeManager.Infrastructure.Services;

// Đơn mang đi của khách (UC02, UC04). Máy chủ tự tính giá, ghi ảnh chụp tên/giá, chống gửi trùng bằng RequestId.
// Đơn Web mới ở trạng thái Pending với lượt nháp: chưa giữ nguyên liệu, chưa có phiếu pha chế (BR03).
public sealed partial class CustomerOrderService(CafeDbContext db)
{
    public const int MaxQuantityPerLine = 50;
    public const int MaxLines = 30;
    // GIẢ ĐỊNH cần nhóm xác nhận (khớp Web/Models/OrderModels.cs): giờ phục vụ 07:00–22:00 giờ Việt Nam (UTC+7).
    public static readonly TimeSpan OpenFrom = new(7, 0, 0);
    public static readonly TimeSpan OpenTo = new(22, 0, 0);
    private static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);

    [GeneratedRegex(@"^(0|\+84)\d{9}$")] private static partial Regex PhoneRegex();

    private Task LockAsync(string resource, CancellationToken ct) =>
        db.Database.ExecuteSqlInterpolatedAsync($@"
DECLARE @r int;
EXEC @r = sys.sp_getapplock @Resource={resource}, @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=15000;
IF @r < 0 THROW 51000, 'Cannot acquire operation lock', 1;", ct);

    private static string Hash(object payload) => Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(payload)));
    private static string Money(decimal v) => v.ToString("F2", CultureInfo.InvariantCulture);

    public async Task<CreateOrderResponse> CreateAsync(string identityUserId, CreateOrderRequest request, DateTime utcNow, CancellationToken ct = default)
    {
        if (request == null) throw new AppException(ApiErrorCodes.Validation, "Thiếu nội dung đơn.");
        var customer = await db.CustomerProfile.AsNoTracking().SingleOrDefaultAsync(x => x.IdentityUserId == identityUserId, ct)
            ?? throw new AppException(ApiErrorCodes.Forbidden, "Cần tài khoản khách để đặt đơn.");
        Validate(request, utcNow, out var pickupUtc, out var contactName, out var contactPhone);
        var lines = request.Lines;

        var hash = Hash(new
        {
            user = identityUserId, pickup = pickupUtc.ToString("O", CultureInfo.InvariantCulture), contactName, contactPhone,
            lines = lines.Select(l => new
            {
                l.VariantId, l.Quantity, expected = Money(l.ExpectedUnitPrice),
                addons = (l.Addons ?? []).Where(a => a.Quantity > 0).OrderBy(a => a.AddonVariantId).Select(a => new { a.AddonVariantId, a.Quantity }),
            }),
        });

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await LockAsync($"request:{request.RequestId:D}", ct);
        var record = await db.IdempotencyRecord.FindAsync([request.RequestId], ct);
        if (record != null)
        {
            if (record.RequestHash != hash) throw new AppException(ApiErrorCodes.Conflict, "Mã yêu cầu đã được dùng cho nội dung khác.");
            if (record.ResultReference != null)
            {
                var existingId = long.Parse(record.ResultReference, CultureInfo.InvariantCulture);
                var existing = await db.SalesOrder.AsNoTracking().SingleAsync(x => x.OrderId == existingId, ct);
                var totals = await TotalsAsync([existingId], ct);
                await tx.CommitAsync(ct);
                return new CreateOrderResponse(existing.OrderId, existing.OrderCode, existing.PickupCode ?? "", existing.Status, totals.GetValueOrDefault(existingId));
            }
        }

        // Giá và tình trạng bán luôn đọc lại tại máy chủ.
        var ids = lines.SelectMany(l => new[] { l.VariantId }.Concat((l.Addons ?? []).Select(a => a.AddonVariantId))).Distinct().ToList();
        var catalog = await MenuQueryService.Sellable(db, utcNow).Where(x => ids.Contains(x.VariantId)).AsNoTracking().ToDictionaryAsync(x => x.VariantId, ct);
        var productIds = lines.Select(l => catalog.GetValueOrDefault(l.VariantId)?.ProductId ?? 0).Distinct().ToList();
        var allowed = await db.AllowedAddon.AsNoTracking().Where(a => productIds.Contains(a.ProductId)).ToListAsync(ct);

        var changes = new List<PriceChange>();
        var priced = new List<(OrderLineRequest Line, SellableVariant Variant, List<(SellableVariant Addon, int Quantity)> Addons)>();
        foreach (var line in lines)
        {
            if (!catalog.TryGetValue(line.VariantId, out var variant) || variant.VariantType != "Drink")
                throw new AppException(ApiErrorCodes.Validation, "Có món không còn bán.");
            if (line.ExpectedUnitPrice != variant.UnitPrice && changes.All(c => c.VariantId != variant.VariantId))
                changes.Add(new PriceChange(variant.VariantId, line.ExpectedUnitPrice, variant.UnitPrice));
            var addons = new List<(SellableVariant, int)>();
            var seen = new HashSet<long>();
            foreach (var a in (line.Addons ?? []).Where(a => a.Quantity != 0))
            {
                var rule = allowed.FirstOrDefault(x => x.ProductId == variant.ProductId && x.AddonVariantId == a.AddonVariantId);
                if (a.Quantity < 0 || rule == null || a.Quantity > rule.MaxPerUnit || !seen.Add(a.AddonVariantId) ||
                    !catalog.TryGetValue(a.AddonVariantId, out var addon) || addon.VariantType != "Addon")
                    throw new AppException(ApiErrorCodes.Validation, "Tùy chọn thêm không hợp lệ.");
                addons.Add((addon, a.Quantity));
            }
            priced.Add((line, variant, addons));
        }
        if (changes.Count > 0) throw new AppException(ApiErrorCodes.PriceChanged, "Giá đã thay đổi, vui lòng xác nhận lại.", changes);

        var order = new SalesOrder
        {
            OrderCode = "WEB-" + utcNow.ToString("yyMMdd", CultureInfo.InvariantCulture) + "-" + RandomCode(8),
            CustomerId = customer.CustomerId, OrderType = "Web", Status = "Pending", CreatedAt = utcNow, ExpectedPickupAt = pickupUtc,
            ContactName = contactName, ContactPhone = contactPhone,
            PickupCode = RandomNumberGenerator.GetInt32(1000, 10000).ToString(CultureInfo.InvariantCulture),
        };
        db.Add(order); await db.SaveChangesAsync(ct);
        var round = new OrderRound { OrderId = order.OrderId, RoundNumber = 1, Status = "Draft" };
        db.Add(round); await db.SaveChangesAsync(ct);
        decimal total = 0;
        foreach (var (line, variant, addons) in priced)
        {
            var row = new OrderLine
            {
                RoundId = round.RoundId, VariantId = variant.VariantId, Quantity = line.Quantity,
                ProductNameSnapshot = variant.ProductName, VariantNameSnapshot = variant.VariantName, UnitPriceSnapshot = variant.UnitPrice,
            };
            db.Add(row); await db.SaveChangesAsync(ct);
            var perUnit = variant.UnitPrice;
            foreach (var (addon, quantity) in addons)
            {
                db.Add(new OrderLineAddon
                {
                    LineId = row.LineId, AddonVariantId = addon.VariantId, Quantity = quantity,
                    AddonNameSnapshot = addon.ProductName, UnitPriceSnapshot = addon.UnitPrice,
                });
                perUnit += addon.UnitPrice * quantity;
            }
            total += perUnit * line.Quantity;
        }
        await db.SaveChangesAsync(ct);
        if (record == null) { record = new IdempotencyRecord { RequestId = request.RequestId, RequestHash = hash, CreatedAt = utcNow }; db.Add(record); }
        record.ResultReference = order.OrderId.ToString(CultureInfo.InvariantCulture);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return new CreateOrderResponse(order.OrderId, order.OrderCode, order.PickupCode!, order.Status, total);
    }

    private static void Validate(CreateOrderRequest r, DateTime utcNow, out DateTime pickupUtc, out string? contactName, out string? contactPhone)
    {
        if (r.RequestId == Guid.Empty) throw new AppException(ApiErrorCodes.Validation, "Thiếu mã yêu cầu.");
        if (r.Lines == null || r.Lines.Count == 0) throw new AppException(ApiErrorCodes.Validation, "Đơn không có món.");
        if (r.Lines.Count > MaxLines) throw new AppException(ApiErrorCodes.Validation, $"Đơn tối đa {MaxLines} dòng.");
        foreach (var l in r.Lines)
            if (l.Quantity < 1 || l.Quantity > MaxQuantityPerLine)
                throw new AppException(ApiErrorCodes.Validation, $"Số lượng mỗi dòng phải là số nguyên từ 1 đến {MaxQuantityPerLine}.");
        // Hợp đồng quy định thời gian là UTC: giá trị không có múi giờ được hiểu là UTC, không phải giờ của máy chủ.
        pickupUtc = r.PickupAt.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(r.PickupAt, DateTimeKind.Utc) : r.PickupAt.ToUniversalTime();
        if (pickupUtc < utcNow.AddMinutes(-1)) throw new AppException(ApiErrorCodes.Validation, "Giờ nhận không được ở quá khứ.");
        if (pickupUtc > utcNow.AddDays(30)) throw new AppException(ApiErrorCodes.Validation, "Giờ nhận quá xa (tối đa 30 ngày).");
        var time = (pickupUtc + VietnamOffset).TimeOfDay;
        if (time < OpenFrom || time > OpenTo)
            throw new AppException(ApiErrorCodes.Validation, $"Giờ nhận phải trong giờ phục vụ ({OpenFrom:hh\\:mm}–{OpenTo:hh\\:mm}).");
        contactName = string.IsNullOrWhiteSpace(r.ContactName) ? null : r.ContactName.Trim();
        contactPhone = string.IsNullOrWhiteSpace(r.ContactPhone) ? null : r.ContactPhone.Trim();
        if (contactName is { Length: > 200 }) throw new AppException(ApiErrorCodes.Validation, "Tên liên hệ tối đa 200 ký tự.");
        if (contactPhone != null && !PhoneRegex().IsMatch(contactPhone)) throw new AppException(ApiErrorCodes.Validation, "Số điện thoại không hợp lệ.");
    }

    private static string RandomCode(int length)
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        return string.Create(length, 0, (span, _) => { for (var i = 0; i < span.Length; i++) span[i] = alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)]; });
    }

    private async Task<CustomerProfile> RequireCustomerAsync(string identityUserId, CancellationToken ct) =>
        await db.CustomerProfile.AsNoTracking().SingleOrDefaultAsync(x => x.IdentityUserId == identityUserId, ct)
        ?? throw new AppException(ApiErrorCodes.Forbidden, "Cần tài khoản khách.");

    public async Task<IReadOnlyList<OrderSummaryDto>> ListMineAsync(string identityUserId, CancellationToken ct = default)
    {
        var customer = await RequireCustomerAsync(identityUserId, ct);
        var orders = await db.SalesOrder.AsNoTracking().Where(x => x.CustomerId == customer.CustomerId)
            .OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.OrderId).Take(200).ToListAsync(ct);
        var totals = await TotalsAsync(orders.Select(o => o.OrderId).ToList(), ct);
        return orders.Select(o => Summary(o, totals.GetValueOrDefault(o.OrderId))).ToList();
    }

    // Đơn của người khác hoặc không tồn tại đều trả null (API trả 404, không lộ sự tồn tại của đơn).
    public async Task<OrderDetailDto?> GetMineAsync(string identityUserId, long orderId, CancellationToken ct = default)
    {
        var customer = await RequireCustomerAsync(identityUserId, ct);
        var owned = await db.SalesOrder.AsNoTracking().AnyAsync(x => x.OrderId == orderId && x.CustomerId == customer.CustomerId, ct);
        return owned ? (await DetailsAsync([orderId], ct)).GetValueOrDefault(orderId) : null;
    }

    public async Task CancelAsync(string identityUserId, long orderId, CancellationToken ct = default)
    {
        var customer = await RequireCustomerAsync(identityUserId, ct);
        var order = await db.SalesOrder.SingleOrDefaultAsync(x => x.OrderId == orderId && x.CustomerId == customer.CustomerId, ct)
            ?? throw new AppException(ApiErrorCodes.NotFound, "Không tìm thấy đơn.");
        if (order.Status != "Pending")
            throw new AppException(ApiErrorCodes.InvalidState, "Đơn đã được tiếp nhận hoặc đã đóng, không thể tự hủy. Vui lòng liên hệ nhân viên.");
        order.Status = "Cancelled";
        foreach (var round in await db.OrderRound.Where(x => x.OrderId == orderId && x.Status == "Draft").ToListAsync(ct)) round.Status = "Cancelled";
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { throw new AppException(ApiErrorCodes.Conflict, "Đơn vừa được xử lý, vui lòng tải lại."); }
    }

    internal static OrderSummaryDto Summary(SalesOrder o, decimal total) => new(o.OrderId, o.OrderCode, o.Status, o.CreatedAt, o.ExpectedPickupAt, total);

    // Tổng tiền tính từ dòng và tùy chọn đã chốt (không lưu tổng trong SalesOrder).
    internal async Task<Dictionary<long, decimal>> TotalsAsync(IReadOnlyCollection<long> orderIds, CancellationToken ct)
    {
        var lines = await (from l in db.OrderLine join r in db.OrderRound on l.RoundId equals r.RoundId where orderIds.Contains(r.OrderId)
                           select new { r.OrderId, l.LineId, l.Quantity, l.UnitPriceSnapshot }).AsNoTracking().ToListAsync(ct);
        var lineIds = lines.Select(l => l.LineId).ToList();
        var addons = await db.OrderLineAddon.AsNoTracking().Where(a => lineIds.Contains(a.LineId)).ToListAsync(ct);
        return lines.GroupBy(l => l.OrderId).ToDictionary(g => g.Key, g => g.Sum(l =>
            (l.UnitPriceSnapshot + addons.Where(a => a.LineId == l.LineId).Sum(a => a.UnitPriceSnapshot * a.Quantity)) * l.Quantity));
    }

    internal async Task<Dictionary<long, OrderDetailDto>> DetailsAsync(IReadOnlyCollection<long> orderIds, CancellationToken ct)
    {
        var orders = await db.SalesOrder.AsNoTracking().Where(o => orderIds.Contains(o.OrderId)).ToListAsync(ct);
        var totals = await TotalsAsync(orderIds, ct);
        var lines = await (from l in db.OrderLine join r in db.OrderRound on l.RoundId equals r.RoundId where orderIds.Contains(r.OrderId)
                           orderby r.RoundNumber, l.LineId select new { r.OrderId, Line = l }).AsNoTracking().ToListAsync(ct);
        var lineIds = lines.Select(x => x.Line.LineId).ToList();
        var addons = await db.OrderLineAddon.AsNoTracking().Where(a => lineIds.Contains(a.LineId)).OrderBy(a => a.LineAddonId).ToListAsync(ct);
        return orders.ToDictionary(o => o.OrderId, o => new OrderDetailDto(Summary(o, totals.GetValueOrDefault(o.OrderId)), o.PickupCode, o.ContactName, o.ContactPhone, o.RejectReason,
            lines.Where(x => x.OrderId == o.OrderId).Select(x => new OrderLineDto(x.Line.ProductNameSnapshot, x.Line.VariantNameSnapshot, x.Line.Quantity, x.Line.UnitPriceSnapshot,
                addons.Where(a => a.LineId == x.Line.LineId).Select(a => new OrderAddonDto(a.AddonNameSnapshot, a.Quantity, a.UnitPriceSnapshot)).ToList())).ToList()));
    }
}
