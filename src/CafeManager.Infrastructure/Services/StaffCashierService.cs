using System.Data;
using CafeManager.Core;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CafeManager.Infrastructure.Services;

// Ca và thu tiền mặt (A2, màn hình Thu ngân). Quyền PAYMENT_COLLECT được kiểm tra ở lớp Web.
// Quy tắc: mỗi nhân viên và mỗi quầy chỉ có một ca mở; thu tiền luôn ghi vào ca đang mở của chính nhân viên đăng nhập;
// chỉ thu đơn đã tiếp nhận còn nợ, số ghi sổ là số còn phải thu (tiền thối không vào sổ); đóng ca lấy cùng khóa hàng Shift
// như lúc thu tiền (BR11) nên một lần thu đang chạy không lọt vào ca vừa đóng.
public sealed class StaffCashierService(CafeDbContext db, ICafeDataCommands commands)
{
    private const decimal MaxCash = 1_000_000_000m;

    private Task LockAsync(string resource, CancellationToken ct) =>
        db.Database.ExecuteSqlInterpolatedAsync($@"
DECLARE @r int;
EXEC @r = sys.sp_getapplock @Resource={resource}, @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=15000;
IF @r < 0 THROW 51000, 'Cannot acquire operation lock', 1;", ct);

    private async Task<StaffAccount> RequireStaffAsync(string identityUserId, CancellationToken ct) =>
        await db.StaffAccount.AsNoTracking().SingleOrDefaultAsync(x => x.IdentityUserId == identityUserId && x.IsActive, ct)
        ?? throw new AppException(ApiErrorCodes.Forbidden, "Cần tài khoản nhân viên đang hoạt động.");

    private static bool ValidMoney(decimal value) => value >= 0 && value <= MaxCash && decimal.Round(value, 2) == value;

    private static AppException Busy() => new(ApiErrorCodes.Conflict, "Hệ thống đang bận xử lý thao tác khác, vui lòng thử lại sau giây lát.");

    public async Task<IReadOnlyList<CounterDto>> CountersAsync(CancellationToken ct = default) =>
        await db.Counter.AsNoTracking().Where(c => c.IsActive).OrderBy(c => c.CounterCode)
            .Select(c => new CounterDto(c.CounterId, c.CounterCode)).ToListAsync(ct);

    private async Task<ShiftDto> ToDtoAsync(Shift shift, CancellationToken ct)
    {
        var counter = await db.Counter.AsNoTracking().SingleAsync(c => c.CounterId == shift.CounterId, ct);
        var opener = await db.StaffAccount.AsNoTracking().SingleAsync(s => s.StaffId == shift.OpenedBy, ct);
        return new ShiftDto(shift.ShiftId, shift.CounterId, counter.CounterCode, opener.FullName, shift.OpenedAt, shift.OpeningCash, shift.ClosedAt, shift.CountedCash);
    }

    public async Task<CurrentShiftResponse> CurrentShiftAsync(string identityUserId, CancellationToken ct = default)
    {
        var staff = await RequireStaffAsync(identityUserId, ct);
        var shift = await db.Shift.AsNoTracking().SingleOrDefaultAsync(s => s.OpenedBy == staff.StaffId && s.ClosedAt == null, ct);
        return new CurrentShiftResponse(shift == null ? null : await ToDtoAsync(shift, ct));
    }

    public async Task<ShiftDto> OpenShiftAsync(string identityUserId, OpenShiftRequest request, DateTime utcNow, CancellationToken ct = default)
    {
        var staff = await RequireStaffAsync(identityUserId, ct);
        if (request == null || !ValidMoney(request.OpeningCash))
            throw new AppException(ApiErrorCodes.Validation, "Tiền đầu ca phải từ 0, tối đa 2 chữ số thập phân.");
        if (!await db.Counter.AnyAsync(c => c.CounterId == request.CounterId && c.IsActive, ct))
            throw new AppException(ApiErrorCodes.Validation, "Quầy không tồn tại hoặc đã ngừng hoạt động.");
        try
        {
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
            await LockAsync("shift-open", ct);
            if (await db.Shift.AnyAsync(s => s.OpenedBy == staff.StaffId && s.ClosedAt == null, ct))
                throw new AppException(ApiErrorCodes.Conflict, "Bạn đang có một ca mở; hãy đóng ca đó trước.");
            if (await db.Shift.AnyAsync(s => s.CounterId == request.CounterId && s.ClosedAt == null, ct))
                throw new AppException(ApiErrorCodes.Conflict, "Quầy này đang có ca mở.");
            var shift = new Shift { CounterId = request.CounterId, OpenedBy = staff.StaffId, OpenedAt = utcNow, OpeningCash = request.OpeningCash };
            db.Add(shift);
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return await ToDtoAsync(shift, ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            throw new AppException(ApiErrorCodes.Conflict, "Quầy này đang có ca mở.");
        }
        catch (Exception ex) when (ex is not AppException && SqlErrors.IsContention(ex)) { throw Busy(); }
    }

    // Ca của người khác hoặc không tồn tại đều trả 404 (không lộ sự tồn tại).
    private async Task<Shift> OwnShiftAsync(StaffAccount staff, long shiftId, CancellationToken ct) =>
        await db.Shift.AsNoTracking().SingleOrDefaultAsync(s => s.ShiftId == shiftId && s.OpenedBy == staff.StaffId, ct)
        ?? throw new AppException(ApiErrorCodes.NotFound, "Không tìm thấy ca.");

    private async Task<ShiftSummaryDto> SummaryAsync(long shiftId, CancellationToken ct)
    {
        var r = await commands.ReconcileShiftAsync(shiftId, ct);
        return new ShiftSummaryDto(shiftId, r.OpeningCash, r.CashPayments, r.CashRefunds, r.OtherIn, r.OtherOut, r.ExpectedCash, r.CountedCash, r.Difference);
    }

    public async Task<ShiftSummaryDto> ShiftSummaryAsync(string identityUserId, long shiftId, CancellationToken ct = default)
    {
        var staff = await RequireStaffAsync(identityUserId, ct);
        await OwnShiftAsync(staff, shiftId, ct);
        return await SummaryAsync(shiftId, ct);
    }

    public async Task<ShiftSummaryDto> CloseShiftAsync(string identityUserId, long shiftId, CloseShiftRequest request, DateTime utcNow, CancellationToken ct = default)
    {
        var staff = await RequireStaffAsync(identityUserId, ct);
        await OwnShiftAsync(staff, shiftId, ct);
        if (request == null || !ValidMoney(request.CountedCash))
            throw new AppException(ApiErrorCodes.Validation, "Tiền đếm thực tế phải từ 0, tối đa 2 chữ số thập phân.");
        var note = string.IsNullOrWhiteSpace(request.CloseNote) ? null : request.CloseNote.Trim();
        if (note is { Length: > 500 }) throw new AppException(ApiErrorCodes.Validation, "Ghi chú tối đa 500 ký tự.");
        try
        {
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            // Cùng khóa hàng với RequireOpenShiftAsync của lúc thu tiền: lần thu đang chạy xong trước, lần thu sau thấy ca đã đóng.
            var shift = await db.Shift.FromSqlInterpolated($"SELECT * FROM dbo.Shift WITH (UPDLOCK,HOLDLOCK) WHERE ShiftId={shiftId}").SingleAsync(ct);
            if (shift.ClosedAt != null) throw new AppException(ApiErrorCodes.InvalidState, "Ca này đã đóng.");
            shift.CountedCash = request.CountedCash; shift.ClosedBy = staff.StaffId; shift.ClosedAt = utcNow; shift.CloseNote = note;
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (DbUpdateConcurrencyException) { throw new AppException(ApiErrorCodes.Conflict, "Ca vừa thay đổi ở nơi khác, vui lòng tải lại."); }
        catch (Exception ex) when (ex is not AppException && SqlErrors.IsContention(ex)) { throw Busy(); }
        return await SummaryAsync(shiftId, ct);
    }

    // Đơn đã tiếp nhận còn phải thu: tổng các lượt đã tiếp nhận trừ số đã thu thành công.
    public async Task<IReadOnlyList<PayableOrderDto>> PayableOrdersAsync(CancellationToken ct = default)
    {
        var orders = await db.SalesOrder.AsNoTracking().Where(o => o.Status == "Serving" || o.Status == "Settling")
            .OrderBy(o => o.ExpectedPickupAt).ThenBy(o => o.OrderId).Take(500).ToListAsync(ct);
        var ids = orders.Select(o => o.OrderId).ToList();
        var totals = await OrderTotals.ComputeAsync(db, ids, acceptedOnly: true, ct);
        var paid = await db.Payment.AsNoTracking().Where(p => ids.Contains(p.OrderId) && p.Status == "Succeeded")
            .GroupBy(p => p.OrderId).Select(g => new { OrderId = g.Key, Amount = g.Sum(p => p.Amount) }).ToDictionaryAsync(x => x.OrderId, x => x.Amount, ct);
        return orders.Select(o => new PayableOrderDto(o.OrderId, o.OrderCode, o.ContactName, o.PickupCode, o.Status,
                totals.GetValueOrDefault(o.OrderId), paid.GetValueOrDefault(o.OrderId), totals.GetValueOrDefault(o.OrderId) - paid.GetValueOrDefault(o.OrderId)))
            .Where(o => o.Remaining > 0).ToList();
    }

    public async Task<CashPaymentResponse> PayCashAsync(string identityUserId, CashPaymentRequest request, CancellationToken ct = default)
    {
        var staff = await RequireStaffAsync(identityUserId, ct);
        if (request == null || request.RequestId == Guid.Empty) throw new AppException(ApiErrorCodes.Validation, "Thiếu mã yêu cầu.");
        if (request.AmountReceived <= 0 || !ValidMoney(request.AmountReceived))
            throw new AppException(ApiErrorCodes.Validation, "Tiền khách đưa phải lớn hơn 0, tối đa 2 chữ số thập phân.");
        if (!await db.SalesOrder.AnyAsync(o => o.OrderId == request.OrderId, ct)) throw new AppException(ApiErrorCodes.NotFound, "Không tìm thấy đơn.");

        // Gửi lại cùng mã yêu cầu phải trả đúng phiếu cũ, kể cả khi ca đã đóng ngay sau lần thu đầu.
        long shiftId;
        var earlier = await db.Payment.AsNoTracking().SingleOrDefaultAsync(p => p.RequestId == request.RequestId, ct);
        if (earlier != null)
        {
            if (earlier.OrderId != request.OrderId || earlier.CreatedBy != staff.StaffId)
                throw new AppException(ApiErrorCodes.Conflict, "Mã yêu cầu đã được dùng cho nội dung khác.");
            shiftId = earlier.ShiftId;
        }
        else
        {
            var shift = await db.Shift.AsNoTracking().SingleOrDefaultAsync(s => s.OpenedBy == staff.StaffId && s.ClosedAt == null, ct)
                ?? throw new AppException(ApiErrorCodes.InvalidState, "Bạn chưa mở ca; hãy mở ca trước khi thu tiền.");
            shiftId = shift.ShiftId;
        }

        try
        {
            var result = await commands.CollectCashAsync(request.RequestId, request.OrderId, shiftId, staff.StaffId, request.AmountReceived, ct);
            return new CashPaymentResponse(result.PaymentId, result.AmountDue, result.AmountReceived, result.Change);
        }
        catch (ArgumentOutOfRangeException)
        {
            throw new AppException(ApiErrorCodes.Validation, "Tiền khách đưa ít hơn số phải trả.");
        }
        catch (Exception ex) when (SqlErrors.IsContention(ex)) { throw Busy(); }
        catch (InvalidOperationException ex) when (ex.Message.StartsWith("RequestId reused", StringComparison.Ordinal))
        {
            throw new AppException(ApiErrorCodes.Conflict, "Mã yêu cầu đã được dùng cho nội dung khác.");
        }
        catch (InvalidOperationException ex) when (ex.Message.StartsWith("Closed shift", StringComparison.Ordinal))
        {
            throw new AppException(ApiErrorCodes.InvalidState, "Ca đã đóng; hãy mở ca mới để thu tiền.");
        }
        catch (InvalidOperationException)
        {
            throw new AppException(ApiErrorCodes.InvalidState, "Đơn chưa được tiếp nhận hoặc không còn khoản phải thu.");
        }
    }
}
