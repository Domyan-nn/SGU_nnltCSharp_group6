using System.Data;
using CafeManager.Core;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CafeManager.Infrastructure.Services;

// Kho nguyên liệu (UC13): xem tồn, nhập kho, kiểm kê và duyệt chênh lệch. Quyền kiểm ở lớp Web:
// INVENTORY_WORK cho xem, nhập, lập phiếu; INVENTORY_APPROVE cho duyệt/từ chối.
// Quy tắc:
// - Nhập kho ghi một lô IngredientBatch (chứng từ nhập), một dòng sổ kho Receipt trỏ về lô đó và cộng tồn sổ trong cùng transaction.
// - Kiểm kê lưu tồn sổ lúc đếm (BookQty), lượng thực đếm và người đếm; tồn KHÔNG đổi cho tới khi được duyệt.
// - Người duyệt phải khác người lập phiếu (CSDL cũng chặn bằng CK_StockCount_3). Duyệt áp chênh lệch (thực đếm − tồn sổ lúc đếm)
//   vào tồn hiện tại bằng một dòng sổ kho AdjustmentIn/AdjustmentOut; các biến động sau lúc đếm đã nằm trong sổ nên không bị tính lại.
// - Điều chỉnh giảm không được làm tồn sổ thấp hơn lượng đang giữ cho đơn (tồn khả dụng không âm, BR08).
// - Mỗi nguyên liệu chỉ có một phiếu chờ duyệt. Mọi thao tác ghi nhật ký AuditEvent (người, thời điểm, ghi chú hoặc lý do).
public sealed class StaffInventoryService(CafeDbContext db)
{
    public const decimal MaxQuantity = 1_000_000m;
    private const int MaxNote = 500, MaxList = 500, MaxCounts = 200;

    private Task LockAsync(string resource, CancellationToken ct) =>
        db.Database.ExecuteSqlInterpolatedAsync($@"
DECLARE @r int;
EXEC @r = sys.sp_getapplock @Resource={resource}, @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=15000;
IF @r < 0 THROW 51000, 'Cannot acquire operation lock', 1;", ct);

    private static bool IsDuplicate(DbUpdateException ex) => ex.InnerException is SqlException { Number: 2601 or 2627 };

    private async Task<StaffAccount> RequireStaffAsync(string identityUserId, CancellationToken ct) =>
        await db.StaffAccount.AsNoTracking().SingleOrDefaultAsync(s => s.IdentityUserId == identityUserId && s.IsActive, ct)
        ?? throw new AppException(ApiErrorCodes.Forbidden, "Cần tài khoản nhân viên đang hoạt động.");

    private static bool ValidQuantity(decimal value) => value <= MaxQuantity && decimal.Round(value, 3) == value;

    private static string? Note(string? text)
    {
        var note = string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        if (note is { Length: > MaxNote }) throw new AppException(ApiErrorCodes.Validation, $"Ghi chú tối đa {MaxNote} ký tự.");
        return note;
    }

    private static string CountCode(long countId) => "CNT-" + countId;

    private void Audit(long staffId, string action, string entity, string reference, string? reason, DateTime utcNow) =>
        db.Add(new AuditEvent { StaffId = staffId, ActionCode = action, EntityName = entity, EntityReference = reference, Reason = reason, CreatedAt = utcNow });

    private Task<decimal> OnHandAsync(long ingredientId, CancellationToken ct) =>
        db.StockBalance.AsNoTracking().Where(s => s.IngredientId == ingredientId).Select(s => s.QuantityOnHand).SingleOrDefaultAsync(ct);

    // ---------------------------------------------------------------- Danh sách tồn

    public async Task<IReadOnlyList<InventoryItemDto>> ItemsAsync(string? search, CancellationToken ct = default)
    {
        var query = db.Ingredient.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search)) { var q = search.Trim(); query = query.Where(i => i.IngredientName.Contains(q)); }
        var items = await query.OrderBy(i => i.IngredientName).Take(MaxList).ToListAsync(ct);
        var ids = items.Select(i => i.IngredientId).ToList();
        var stock = await db.StockBalance.AsNoTracking().Where(s => ids.Contains(s.IngredientId)).ToDictionaryAsync(s => s.IngredientId, ct);
        var pending = await db.StockCount.AsNoTracking().Where(c => ids.Contains(c.IngredientId) && c.Status == StockCountStatuses.Pending)
            .ToDictionaryAsync(c => c.IngredientId, c => c.CountId, ct);
        return items.Select(i =>
        {
            var onHand = stock.TryGetValue(i.IngredientId, out var s) ? s.QuantityOnHand : 0;
            var reserved = s?.QuantityReserved ?? 0;
            return new InventoryItemDto(i.IngredientId, i.IngredientName, i.UnitName, i.IsActive, onHand, reserved, onHand - reserved,
                pending.TryGetValue(i.IngredientId, out var countId) ? countId : null);
        }).ToList();
    }

    // ---------------------------------------------------------------- Nhập kho

    // Mã sổ kho suy ra từ RequestId nên gửi lại cùng mã trả kết quả cũ; cùng mã nhưng khác nguyên liệu hoặc số lượng bị từ chối.
    public async Task<StockReceiptResponse> ReceiveAsync(string identityUserId, long ingredientId, StockReceiptRequest request,
        DateTime utcNow, CancellationToken ct = default)
    {
        var staff = await RequireStaffAsync(identityUserId, ct);
        if (request == null || request.RequestId == Guid.Empty) throw new AppException(ApiErrorCodes.Validation, "Thiếu mã yêu cầu.");
        var quantity = request.Quantity;
        if (quantity <= 0 || !ValidQuantity(quantity))
            throw new AppException(ApiErrorCodes.Validation, "Số lượng nhập phải lớn hơn 0, tối đa 1.000.000 và tối đa 3 chữ số thập phân.");
        var note = Note(request.Note);

        var code = "RCV-" + request.RequestId.ToString("N");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        await LockAsync("stock:receipt:" + code, ct);

        var earlier = await db.StockMovement.AsNoTracking().SingleOrDefaultAsync(m => m.MovementCode == code, ct);
        if (earlier != null)
        {
            if (earlier.IngredientId != ingredientId || earlier.Quantity != quantity)
                throw new AppException(ApiErrorCodes.Conflict, "Mã yêu cầu đã được dùng cho nội dung khác.");
            return new StockReceiptResponse(earlier.SourceId, earlier.MovementId, await OnHandAsync(ingredientId, ct));
        }

        var ingredient = await db.Ingredient.AsNoTracking().SingleOrDefaultAsync(i => i.IngredientId == ingredientId, ct)
            ?? throw new AppException(ApiErrorCodes.NotFound, "Không tìm thấy nguyên liệu.");
        if (!ingredient.IsActive)
            throw new AppException(ApiErrorCodes.InvalidState, "Nguyên liệu đang ngừng sử dụng; hãy bật lại \"Đang dùng\" ở màn Nguyên liệu trước khi nhập kho.");

        var batch = new IngredientBatch { IngredientId = ingredientId, QuantityReceived = quantity };
        db.Add(batch);
        await db.SaveChangesAsync(ct);
        var movement = new StockMovement
        {
            MovementCode = code, IngredientId = ingredientId, MovementType = "Receipt", Quantity = quantity,
            SourceType = "IngredientBatch", SourceId = batch.BatchId, CreatedAt = utcNow,
        };
        db.Add(movement);
        Audit(staff.StaffId, "STOCK_RECEIPT", "IngredientBatch", code, note, utcNow);
        // Cộng tồn bằng một câu UPDATE nguyên tử để không mất lượng khi hai người cùng nhập; nguyên liệu cũ chưa có dòng tồn thì tạo mới.
        var updated = await db.StockBalance.Where(s => s.IngredientId == ingredientId)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.QuantityOnHand, x => x.QuantityOnHand + quantity), ct);
        if (updated == 0) db.Add(new StockBalance { IngredientId = ingredientId, QuantityOnHand = quantity, QuantityReserved = 0 });
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException ex) when (IsDuplicate(ex)) { throw new AppException(ApiErrorCodes.Conflict, "Mã yêu cầu đã được dùng cho nội dung khác."); }
        var onHand = await OnHandAsync(ingredientId, ct);
        await tx.CommitAsync(ct);
        return new StockReceiptResponse(batch.BatchId, movement.MovementId, onHand);
    }

    // ---------------------------------------------------------------- Kiểm kê

    public async Task<StockCountDto> CreateCountAsync(string identityUserId, long ingredientId, CreateStockCountRequest request,
        DateTime utcNow, CancellationToken ct = default)
    {
        var staff = await RequireStaffAsync(identityUserId, ct);
        if (request == null || request.CountedQty < 0 || !ValidQuantity(request.CountedQty))
            throw new AppException(ApiErrorCodes.Validation, "Lượng thực đếm phải từ 0 đến 1.000.000, tối đa 3 chữ số thập phân.");
        var note = Note(request.Note);
        if (!await db.Ingredient.AnyAsync(i => i.IngredientId == ingredientId, ct))
            throw new AppException(ApiErrorCodes.NotFound, "Không tìm thấy nguyên liệu.");

        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        await LockAsync("stock:ingredient:" + ingredientId, ct);
        var pending = await db.StockCount.AsNoTracking()
            .Where(c => c.IngredientId == ingredientId && c.Status == StockCountStatuses.Pending).Select(c => (long?)c.CountId).FirstOrDefaultAsync(ct);
        if (pending != null)
            throw new AppException(ApiErrorCodes.Conflict, $"Nguyên liệu đang có phiếu kiểm kê {CountCode(pending.Value)} chờ duyệt; hãy chờ duyệt hoặc từ chối phiếu đó trước.");

        var count = new StockCount
        {
            IngredientId = ingredientId, BookQty = await OnHandAsync(ingredientId, ct), CountedQty = request.CountedQty,
            CountedAt = utcNow, CountedBy = staff.StaffId, Status = StockCountStatuses.Pending,
        };
        db.Add(count);
        await db.SaveChangesAsync(ct);
        Audit(staff.StaffId, "STOCK_COUNT", "StockCount", CountCode(count.CountId), note, utcNow);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return await CountAsync(count.CountId, ct);
    }

    public async Task<IReadOnlyList<StockCountDto>> CountsAsync(string? status, CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(status) && !StockCountStatuses.All.Contains(status))
            throw new AppException(ApiErrorCodes.Validation, "Trạng thái phiếu kiểm kê không hợp lệ.");
        var query = db.StockCount.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(c => c.Status == status);
        var ids = await query.OrderByDescending(c => c.CountedAt).ThenByDescending(c => c.CountId).Take(MaxCounts).Select(c => c.CountId).ToListAsync(ct);
        return await ProjectAsync(ids, ct);
    }

    public async Task<StockCountDto> ApproveAsync(string identityUserId, long countId, DateTime utcNow, CancellationToken ct = default)
    {
        var approver = await RequireStaffAsync(identityUserId, ct);
        var ingredientId = await db.StockCount.AsNoTracking().Where(c => c.CountId == countId).Select(c => (long?)c.IngredientId).SingleOrDefaultAsync(ct)
            ?? throw new AppException(ApiErrorCodes.NotFound, "Không tìm thấy phiếu kiểm kê.");

        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        await LockAsync("stock:ingredient:" + ingredientId, ct);
        var count = await db.StockCount.SingleAsync(c => c.CountId == countId, ct);
        RequirePendingByAnother(count, approver);

        var delta = count.CountedQty - count.BookQty;
        if (delta != 0)
        {
            var amount = Math.Abs(delta);
            int changed;
            if (delta > 0)
            {
                changed = await db.StockBalance.Where(s => s.IngredientId == ingredientId)
                    .ExecuteUpdateAsync(s => s.SetProperty(x => x.QuantityOnHand, x => x.QuantityOnHand + amount), ct);
                if (changed == 0) db.Add(new StockBalance { IngredientId = ingredientId, QuantityOnHand = amount, QuantityReserved = 0 });
            }
            else
            {
                changed = await db.StockBalance.Where(s => s.IngredientId == ingredientId && s.QuantityOnHand - amount >= s.QuantityReserved)
                    .ExecuteUpdateAsync(s => s.SetProperty(x => x.QuantityOnHand, x => x.QuantityOnHand - amount), ct);
                if (changed == 0)
                {
                    var reserved = await db.StockBalance.AsNoTracking().Where(s => s.IngredientId == ingredientId).Select(s => s.QuantityReserved).SingleOrDefaultAsync(ct);
                    throw new AppException(ApiErrorCodes.InvalidState,
                        $"Giảm {amount:0.###} làm tồn sổ thấp hơn lượng đang giữ cho đơn ({reserved:0.###}). Hãy để các đơn đang giữ được pha chế hoặc xử lý trước, rồi kiểm kê lại.");
                }
            }
            db.Add(new StockMovement
            {
                MovementCode = CountCode(count.CountId), IngredientId = ingredientId, MovementType = delta > 0 ? "AdjustmentIn" : "AdjustmentOut",
                Quantity = amount, SourceType = "StockCount", SourceId = count.CountId, CreatedAt = utcNow,
            });
        }
        count.Status = StockCountStatuses.Approved;
        count.ApprovedBy = approver.StaffId;
        Audit(approver.StaffId, "STOCK_COUNT_APPROVE", "StockCount", CountCode(count.CountId), null, utcNow);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return await CountAsync(countId, ct);
    }

    public async Task<StockCountDto> RejectAsync(string identityUserId, long countId, RejectStockCountRequest request, DateTime utcNow, CancellationToken ct = default)
    {
        var approver = await RequireStaffAsync(identityUserId, ct);
        var reason = request?.Reason?.Trim();
        if (string.IsNullOrEmpty(reason)) throw new AppException(ApiErrorCodes.Validation, "Nhập lý do từ chối.");
        if (reason.Length > MaxNote) throw new AppException(ApiErrorCodes.Validation, $"Lý do tối đa {MaxNote} ký tự.");
        var ingredientId = await db.StockCount.AsNoTracking().Where(c => c.CountId == countId).Select(c => (long?)c.IngredientId).SingleOrDefaultAsync(ct)
            ?? throw new AppException(ApiErrorCodes.NotFound, "Không tìm thấy phiếu kiểm kê.");

        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        await LockAsync("stock:ingredient:" + ingredientId, ct);
        var count = await db.StockCount.SingleAsync(c => c.CountId == countId, ct);
        RequirePendingByAnother(count, approver);
        count.Status = StockCountStatuses.Rejected;
        count.ApprovedBy = approver.StaffId;
        Audit(approver.StaffId, "STOCK_COUNT_REJECT", "StockCount", CountCode(count.CountId), reason, utcNow);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return await CountAsync(countId, ct);
    }

    private static void RequirePendingByAnother(StockCount count, StaffAccount approver)
    {
        if (count.Status != StockCountStatuses.Pending)
            throw new AppException(ApiErrorCodes.InvalidState, "Phiếu kiểm kê này đã được xử lý.");
        if (count.CountedBy == approver.StaffId)
            throw new AppException(ApiErrorCodes.InvalidState, "Người lập phiếu kiểm kê không tự duyệt hay từ chối phiếu của mình; cần một quản lý khác.");
    }

    private async Task<StockCountDto> CountAsync(long countId, CancellationToken ct) => (await ProjectAsync([countId], ct)).Single();

    private async Task<IReadOnlyList<StockCountDto>> ProjectAsync(IReadOnlyList<long> countIds, CancellationToken ct)
    {
        var rows = await (from c in db.StockCount.AsNoTracking()
                          where countIds.Contains(c.CountId)
                          join i in db.Ingredient on c.IngredientId equals i.IngredientId
                          join counter in db.StaffAccount on c.CountedBy equals counter.StaffId
                          join a in db.StaffAccount on c.ApprovedBy equals (long?)a.StaffId into approvers
                          from approver in approvers.DefaultIfEmpty()
                          select new
                          {
                              c.CountId, c.IngredientId, i.IngredientName, i.UnitName, c.BookQty, c.CountedQty, c.CountedAt,
                              CountedByName = counter.FullName, c.Status, ApprovedByName = approver == null ? null : approver.FullName,
                              OnHand = db.StockBalance.Where(s => s.IngredientId == c.IngredientId).Select(s => (decimal?)s.QuantityOnHand).FirstOrDefault() ?? 0,
                          }).ToListAsync(ct);
        var order = countIds.Select((id, index) => (id, index)).ToDictionary(x => x.id, x => x.index);
        return rows.OrderBy(r => order[r.CountId]).Select(r => new StockCountDto(r.CountId, r.IngredientId, r.IngredientName, r.UnitName,
            r.BookQty, r.CountedQty, r.CountedQty - r.BookQty, DateTime.SpecifyKind(r.CountedAt, DateTimeKind.Utc), r.CountedByName, r.Status,
            r.ApprovedByName, r.OnHand)).ToList();
    }
}
