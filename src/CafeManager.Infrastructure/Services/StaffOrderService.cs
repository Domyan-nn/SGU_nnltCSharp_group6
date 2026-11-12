using CafeManager.Core;
using Microsoft.EntityFrameworkCore;

namespace CafeManager.Infrastructure.Services;

// Thu ngân tiếp nhận hoặc từ chối đơn Web (UC03). Việc giữ tồn và tạo phiếu do ICafeDataCommands.AcceptRoundAsync
// thực hiện trong một transaction; ở đây chỉ kiểm tra trạng thái, ghi lý do từ chối, nhật ký và đổi lỗi sang mã nghiệp vụ.
public sealed class StaffOrderService(CafeDbContext db, ICafeDataCommands commands, CustomerOrderService orders)
{
    private static readonly string[] Statuses = ["Pending", "Serving", "Rejected", "Cancelled", "Settling", "Closed"];

    private async Task<StaffAccount> RequireStaffAsync(string identityUserId, CancellationToken ct) =>
        await db.StaffAccount.AsNoTracking().SingleOrDefaultAsync(x => x.IdentityUserId == identityUserId && x.IsActive, ct)
        ?? throw new AppException(ApiErrorCodes.Forbidden, "Cần tài khoản nhân viên đang hoạt động.");

    public async Task<IReadOnlyList<StaffOrderDto>> ListAsync(string? status, CancellationToken ct = default)
    {
        status = string.IsNullOrWhiteSpace(status) ? "Pending" : status;
        if (!Statuses.Contains(status)) throw new AppException(ApiErrorCodes.Validation, "Trạng thái không hợp lệ.");
        var ids = await db.SalesOrder.AsNoTracking().Where(o => o.OrderType == "Web" && o.Status == status)
            .OrderBy(o => o.ExpectedPickupAt).ThenBy(o => o.OrderId).Take(200).Select(o => o.OrderId).ToListAsync(ct);
        var details = await orders.DetailsAsync(ids, ct);
        var rounds = await db.OrderRound.AsNoTracking().Where(r => ids.Contains(r.OrderId)).OrderBy(r => r.RoundNumber).ToListAsync(ct);
        return ids.Select(id => new StaffOrderDto(details[id],
            (rounds.FirstOrDefault(r => r.OrderId == id && r.Status == "Draft") ?? rounds.First(r => r.OrderId == id)).RoundId)).ToList();
    }

    public async Task<AcceptOrderResponse> AcceptAsync(string identityUserId, long orderId, DateTime utcNow, CancellationToken ct = default)
    {
        var staff = await RequireStaffAsync(identityUserId, ct);
        var order = await db.SalesOrder.AsNoTracking().SingleOrDefaultAsync(x => x.OrderId == orderId && x.OrderType == "Web", ct)
            ?? throw new AppException(ApiErrorCodes.NotFound, "Không tìm thấy đơn.");
        if (order.Status is "Cancelled") throw new AppException(ApiErrorCodes.InvalidState, "Khách đã hủy đơn này.");
        if (order.Status is "Rejected") throw new AppException(ApiErrorCodes.InvalidState, "Đơn này đã bị từ chối.");
        var round = await db.OrderRound.AsNoTracking().Where(x => x.OrderId == orderId && (x.Status == "Draft" || x.Status == "Accepted"))
            .OrderBy(x => x.Status == "Draft" ? 0 : 1).ThenBy(x => x.RoundNumber).FirstOrDefaultAsync(ct)
            ?? throw new AppException(ApiErrorCodes.InvalidState, "Đơn không còn lượt chờ tiếp nhận.");
        try
        {
            var ticketId = await commands.AcceptRoundAsync(round.RoundId, utcNow, ct); // gọi lại trên lượt đã nhận trả phiếu cũ
            if (order.Status == "Pending") // gọi lại trên đơn đã tiếp nhận không ghi nhật ký thêm lần nữa
            {
                db.Add(new AuditEvent { StaffId = staff.StaffId, ActionCode = "ORDER_ACCEPT", EntityName = "SalesOrder", EntityReference = order.OrderCode, CreatedAt = utcNow });
                await db.SaveChangesAsync(ct);
            }
            return new AcceptOrderResponse(ticketId);
        }
        catch (InvalidOperationException ex) when (ex.Message.StartsWith("Insufficient available stock", StringComparison.Ordinal))
        {
            throw new AppException(ApiErrorCodes.InsufficientStock, "Không đủ nguyên liệu để tiếp nhận đơn này.");
        }
        catch (InvalidOperationException)
        {
            throw new AppException(ApiErrorCodes.InvalidState, "Không thể tiếp nhận: có món không còn bán, thiếu công thức hoặc đơn vừa đổi trạng thái.");
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new AppException(ApiErrorCodes.Conflict, "Đơn vừa được xử lý ở nơi khác, vui lòng tải lại.");
        }
    }

    public async Task RejectAsync(string identityUserId, long orderId, string? reason, DateTime utcNow, CancellationToken ct = default)
    {
        var staff = await RequireStaffAsync(identityUserId, ct);
        reason = reason?.Trim();
        if (string.IsNullOrEmpty(reason)) throw new AppException(ApiErrorCodes.Validation, "Nhập lý do từ chối.");
        if (reason.Length > 500) throw new AppException(ApiErrorCodes.Validation, "Lý do tối đa 500 ký tự.");
        var order = await db.SalesOrder.SingleOrDefaultAsync(x => x.OrderId == orderId && x.OrderType == "Web", ct)
            ?? throw new AppException(ApiErrorCodes.NotFound, "Không tìm thấy đơn.");
        if (order.Status != "Pending") throw new AppException(ApiErrorCodes.InvalidState, "Chỉ từ chối được đơn đang chờ tiếp nhận.");
        order.Status = "Rejected"; order.RejectReason = reason; order.RejectedBy = staff.StaffId; order.RejectedAt = utcNow;
        foreach (var round in await db.OrderRound.Where(x => x.OrderId == orderId && x.Status == "Draft").ToListAsync(ct)) round.Status = "Cancelled";
        db.Add(new AuditEvent { StaffId = staff.StaffId, ActionCode = "ORDER_REJECT", EntityName = "SalesOrder", EntityReference = order.OrderCode, Reason = reason, CreatedAt = utcNow });
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { throw new AppException(ApiErrorCodes.Conflict, "Đơn vừa được xử lý ở nơi khác, vui lòng tải lại."); }
    }
}
