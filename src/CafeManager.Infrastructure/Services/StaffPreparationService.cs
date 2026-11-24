using System.Data;
using CafeManager.Core;
using Microsoft.EntityFrameworkCore;

namespace CafeManager.Infrastructure.Services;

// Màn hình Pha chế (A3). Quyền PREPARATION_WORK được kiểm tra ở lớp Web.
// Mỗi ly là một PreparationUnit: Waiting -> Preparing -> Ready -> Delivered, mỗi bước chỉ đi tiếp một nấc.
// Bắt đầu pha trừ nguyên liệu qua ICafeDataCommands.StartUnitAsync (một transaction, khóa theo ly); sau mỗi bước, trạng thái phiếu
// (PreparationTicket) được suy ra lại từ các ly của phiếu: Waiting -> Preparing -> Ready -> Completed.
public sealed class StaffPreparationService(CafeDbContext db, ICafeDataCommands commands)
{
    private const int MaxQueue = 500;

    private Task LockAsync(string resource, CancellationToken ct) =>
        db.Database.ExecuteSqlInterpolatedAsync($@"
DECLARE @r int;
EXEC @r = sys.sp_getapplock @Resource={resource}, @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=15000;
IF @r < 0 THROW 51000, 'Cannot acquire operation lock', 1;", ct);

    private async Task<StaffAccount> RequireStaffAsync(string identityUserId, CancellationToken ct) =>
        await db.StaffAccount.AsNoTracking().SingleOrDefaultAsync(x => x.IdentityUserId == identityUserId && x.IsActive, ct)
        ?? throw new AppException(ApiErrorCodes.Forbidden, "Cần tài khoản nhân viên đang hoạt động.");

    // Phiếu của một ly suy ra qua dòng và lượt (PreparationTicket.RoundId là duy nhất); PreparationUnit không lưu TicketId (3NF).
    private Task<long> TicketIdOfAsync(long lineId, CancellationToken ct) =>
        (from l in db.OrderLine
         join t in db.PreparationTicket on l.RoundId equals t.RoundId
         where l.LineId == lineId
         select t.TicketId).SingleAsync(ct);

    private static AppException Busy() => new(ApiErrorCodes.Conflict, "Hệ thống đang bận xử lý thao tác khác, vui lòng thử lại sau giây lát.");
    private static AppException Changed() => new(ApiErrorCodes.Conflict, "Ly vừa được xử lý ở nơi khác, vui lòng tải lại danh sách.");

    // Các ly chưa giao, cũ nhất (theo giờ tiếp nhận) trước.
    public async Task<IReadOnlyList<PreparationUnitDto>> QueueAsync(CancellationToken ct = default)
    {
        var rows = await (from u in db.PreparationUnit
                          join l in db.OrderLine on u.LineId equals l.LineId
                          join r in db.OrderRound on l.RoundId equals r.RoundId
                          join t in db.PreparationTicket on r.RoundId equals t.RoundId
                          join o in db.SalesOrder on r.OrderId equals o.OrderId
                          where u.Status == PreparationStatuses.Waiting || u.Status == PreparationStatuses.Preparing || u.Status == PreparationStatuses.Ready
                          orderby r.AcceptedAt, u.UnitId
                          select new { u.UnitId, t.TicketId, o.OrderId, o.OrderCode, o.PickupCode, l.LineId, l.ProductNameSnapshot, l.VariantNameSnapshot,
                              u.UnitNumber, LineQuantity = l.Quantity, u.Status, r.AcceptedAt })
            .AsNoTracking().Take(MaxQueue).ToListAsync(ct);
        var lineIds = rows.Select(x => x.LineId).Distinct().ToList();
        var addons = await db.OrderLineAddon.AsNoTracking().Where(a => lineIds.Contains(a.LineId)).OrderBy(a => a.LineAddonId).ToListAsync(ct);
        return rows.Select(x => new PreparationUnitDto(x.UnitId, x.TicketId, x.OrderId, x.OrderCode, x.PickupCode, x.ProductNameSnapshot, x.VariantNameSnapshot,
            addons.Where(a => a.LineId == x.LineId).Select(a => new OrderAddonDto(a.AddonNameSnapshot, a.Quantity, a.UnitPriceSnapshot)).ToList(),
            x.UnitNumber, x.LineQuantity, x.Status, DateTime.SpecifyKind(x.AcceptedAt ?? DateTime.UnixEpoch, DateTimeKind.Utc))).ToList();
    }

    public async Task StartAsync(string identityUserId, long unitId, DateTime utcNow, CancellationToken ct = default)
    {
        await RequireStaffAsync(identityUserId, ct);
        var unit = await db.PreparationUnit.AsNoTracking().SingleOrDefaultAsync(u => u.UnitId == unitId, ct)
            ?? throw new AppException(ApiErrorCodes.NotFound, "Không tìm thấy ly.");
        if (unit.Status != PreparationStatuses.Waiting) throw new AppException(ApiErrorCodes.InvalidState, "Ly này không còn ở trạng thái chờ pha chế.");
        try { await commands.StartUnitAsync(unitId, utcNow, ct); }
        catch (Exception ex) when (SqlErrors.IsContention(ex)) { throw Busy(); }
        catch (InvalidOperationException ex) when (ex.Message.StartsWith("Unit must be waiting", StringComparison.Ordinal))
        {
            throw new AppException(ApiErrorCodes.InvalidState, "Ly này vừa được người khác bắt đầu pha.");
        }
        catch (InvalidOperationException)
        {
            throw new AppException(ApiErrorCodes.InvalidState, "Không thể bắt đầu pha: thiếu phần nguyên liệu đã giữ hoặc tồn kho không khớp.");
        }
        catch (DbUpdateConcurrencyException) { throw Changed(); }
        await SyncTicketAsync(await TicketIdOfAsync(unit.LineId, ct), ct);
    }

    public async Task MarkReadyAsync(string identityUserId, long unitId, CancellationToken ct = default)
    {
        await RequireStaffAsync(identityUserId, ct);
        var unit = await db.PreparationUnit.SingleOrDefaultAsync(u => u.UnitId == unitId, ct)
            ?? throw new AppException(ApiErrorCodes.NotFound, "Không tìm thấy ly.");
        if (unit.Status != PreparationStatuses.Preparing) throw new AppException(ApiErrorCodes.InvalidState, "Chỉ báo xong được ly đang pha.");
        unit.Status = PreparationStatuses.Ready;
        var attempt = await db.PreparationAttempt.Where(a => a.UnitId == unitId && a.Outcome == "Started").OrderByDescending(a => a.AttemptNo).FirstOrDefaultAsync(ct);
        if (attempt != null) attempt.Outcome = "Completed";
        await SaveUnitAsync(await TicketIdOfAsync(unit.LineId, ct), ct);
    }

    public async Task DeliverAsync(string identityUserId, long unitId, DateTime utcNow, CancellationToken ct = default)
    {
        var staff = await RequireStaffAsync(identityUserId, ct);
        var unit = await db.PreparationUnit.SingleOrDefaultAsync(u => u.UnitId == unitId, ct)
            ?? throw new AppException(ApiErrorCodes.NotFound, "Không tìm thấy ly.");
        if (unit.Status != PreparationStatuses.Ready) throw new AppException(ApiErrorCodes.InvalidState, "Chỉ giao được ly đã pha xong.");
        unit.Status = PreparationStatuses.Delivered; unit.DeliveredBy = staff.StaffId; unit.DeliveredAt = utcNow;
        await SaveUnitAsync(await TicketIdOfAsync(unit.LineId, ct), ct);

        // PreparationUnit không lưu OrderId (3NF) nên tìm đơn qua dòng và lượt.
        var orderId = await (from l in db.OrderLine join r in db.OrderRound on l.RoundId equals r.RoundId
                             where l.LineId == unit.LineId select r.OrderId).SingleAsync(ct);
        try { await OrderClosing.TryCloseAsync(db, orderId, ct); }
        catch (DbUpdateConcurrencyException) { /* thao tác thu tiền vừa đóng đơn cùng lúc: bỏ qua */ }
    }

    private async Task SaveUnitAsync(long ticketId, CancellationToken ct)
    {
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { throw Changed(); }
        catch (Exception ex) when (SqlErrors.IsContention(ex)) { throw Busy(); }
        await SyncTicketAsync(ticketId, ct);
    }

    internal static string TicketStatusFor(IReadOnlyCollection<string> units)
    {
        var live = units.Where(s => s != PreparationStatuses.Cancelled).ToList();
        if (live.Count == 0 || live.All(s => s == PreparationStatuses.Delivered)) return "Completed";
        if (live.All(s => s is PreparationStatuses.Ready or PreparationStatuses.Delivered)) return "Ready";
        if (live.Any(s => s != PreparationStatuses.Waiting)) return "Preparing";
        return "Waiting";
    }

    // Phiếu luôn suy ra từ trạng thái ly hiện có (không cộng dồn), nên chạy lại bao nhiêu lần cũng cho cùng kết quả;
    // khóa theo phiếu để hai người báo xong ly cuối cùng cùng lúc không ghi đè nhau bằng số liệu cũ.
    private async Task SyncTicketAsync(long ticketId, CancellationToken ct)
    {
        try
        {
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
            await LockAsync($"ticket:{ticketId}", ct);
            var statuses = await (from u in db.PreparationUnit
                                  join l in db.OrderLine on u.LineId equals l.LineId
                                  join t in db.PreparationTicket on l.RoundId equals t.RoundId
                                  where t.TicketId == ticketId
                                  select u.Status).AsNoTracking().ToListAsync(ct);
            var ticket = await db.PreparationTicket.SingleAsync(t => t.TicketId == ticketId, ct);
            var next = TicketStatusFor(statuses);
            if (ticket.Status != "Cancelled" && ticket.Status != next)
            {
                ticket.Status = next;
                await db.SaveChangesAsync(ct);
            }
            await tx.CommitAsync(ct);
        }
        catch (Exception ex) when (SqlErrors.IsContention(ex)) { throw Busy(); }
    }
}
