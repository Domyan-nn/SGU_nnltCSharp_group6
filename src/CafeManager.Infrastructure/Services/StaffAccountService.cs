using System.Data;
using System.Text.RegularExpressions;
using CafeManager.Core;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CafeManager.Infrastructure.Services;

// Quản lý nhân viên và phân quyền (A6, UC16; Desktop). Quyền STAFF_MANAGE được kiểm tra ở lớp Web.
// Danh tính (tên đăng nhập, mật khẩu băm, khóa tạm do nhập sai) do ASP.NET Core Identity giữ; StaffAccount giữ họ tên và trạng thái làm việc;
// quyền thật đến từ StaffRole -> RolePermission nên đổi vai trò có hiệu lực ngay ở yêu cầu kế tiếp (quyền không nằm trong cookie).
// Quy tắc: không xóa nhân viên (đã có chứng từ tham chiếu) mà chỉ khóa/mở; mỗi nhân viên có ít nhất một vai trò;
// không tự khóa mình; luôn còn ít nhất một nhân viên đang hoạt động có quyền STAFF_MANAGE. Mọi thay đổi ghi vào AuditEvent.
public sealed partial class StaffAccountService(CafeDbContext db, UserManager<IdentityUser> users)
{
    private const int MaxList = 500, MaxFullName = 200, MaxRoles = 20;

    [GeneratedRegex(@"^[A-Za-z0-9._-]+$")] private static partial Regex UserNameRegex();

    // Chuỗi hóa mọi thay đổi nhân sự để phép kiểm "còn quản trị viên cuối" không bị hai yêu cầu song song vượt qua.
    private Task LockAsync(CancellationToken ct) =>
        db.Database.ExecuteSqlInterpolatedAsync($@"
DECLARE @r int;
EXEC @r = sys.sp_getapplock @Resource={"staff:accounts"}, @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=15000;
IF @r < 0 THROW 51000, 'Cannot acquire operation lock', 1;", ct);

    private static AppException Invalid(string message) => new(ApiErrorCodes.Validation, message);
    private static bool IsDuplicate(DbUpdateException ex) => ex.InnerException is SqlException { Number: 2601 or 2627 };

    private static string CheckFullName(string? value)
    {
        var text = value?.Trim();
        if (string.IsNullOrEmpty(text)) throw Invalid("Nhập họ tên.");
        if (text.Length > MaxFullName) throw Invalid($"Họ tên tối đa {MaxFullName} ký tự.");
        return text;
    }

    private static List<long> CheckRoleIds(IReadOnlyList<long>? roleIds)
    {
        var ids = (roleIds ?? []).Distinct().ToList();
        if (ids.Count == 0) throw Invalid("Chọn ít nhất một vai trò.");
        if (ids.Count > MaxRoles) throw Invalid($"Tối đa {MaxRoles} vai trò cho một nhân viên.");
        return ids;
    }

    // ---------------------------------------------------------------- Xem

    public async Task<IReadOnlyList<StaffAccountDto>> ListAsync(string currentIdentityUserId, string? search, CancellationToken ct = default)
    {
        var query = from s in db.StaffAccount.AsNoTracking()
                    join u in db.Users.AsNoTracking() on s.IdentityUserId equals u.Id
                    select new { s.StaffId, UserName = u.UserName!, s.FullName, s.IsActive, s.IdentityUserId };
        if (!string.IsNullOrWhiteSpace(search))
        {
            var q = search.Trim();
            query = query.Where(x => x.FullName.Contains(q) || x.UserName.Contains(q));
        }
        var rows = await query.OrderBy(x => x.FullName).ThenBy(x => x.UserName).Take(MaxList).ToListAsync(ct);
        var ids = rows.Select(x => x.StaffId).ToList();
        var assigned = await (from sr in db.StaffRole.AsNoTracking()
                              join r in db.Role.AsNoTracking() on sr.RoleId equals r.RoleId
                              where ids.Contains(sr.StaffId)
                              orderby r.RoleName
                              select new { sr.StaffId, r.RoleId, r.RoleName }).ToListAsync(ct);
        return rows.Select(x =>
        {
            var mine = assigned.Where(a => a.StaffId == x.StaffId).ToList();
            return new StaffAccountDto(x.StaffId, x.UserName, x.FullName, x.IsActive,
                mine.Select(a => a.RoleId).ToList(), mine.Select(a => a.RoleName).ToList(), x.IdentityUserId == currentIdentityUserId);
        }).ToList();
    }

    public async Task<IReadOnlyList<RoleDto>> RolesAsync(CancellationToken ct = default)
    {
        var roles = await db.Role.AsNoTracking().OrderBy(r => r.RoleName).ToListAsync(ct);
        var grants = await (from rp in db.RolePermission.AsNoTracking()
                            join p in db.Permission.AsNoTracking() on rp.PermissionId equals p.PermissionId
                            select new { rp.RoleId, p.PermissionCode }).ToListAsync(ct);
        return roles.Select(r => new RoleDto(r.RoleId, r.RoleName,
            grants.Where(g => g.RoleId == r.RoleId).Select(g => g.PermissionCode).OrderBy(c => c).ToList())).ToList();
    }

    // ---------------------------------------------------------------- Tạo

    public async Task<long> CreateAsync(string actorIdentityUserId, CreateStaffRequest request, DateTime utcNow, CancellationToken ct = default)
    {
        var userName = (request?.UserName ?? "").Trim();
        if (userName.Length is < 3 or > 50 || !UserNameRegex().IsMatch(userName))
            throw Invalid("Tên đăng nhập 3 đến 50 ký tự gồm chữ không dấu, số, dấu chấm, gạch dưới hoặc gạch nối.");
        var password = request!.Password ?? "";
        if (password.Length is < 6 or > 100) throw Invalid("Mật khẩu từ 6 đến 100 ký tự.");
        var fullName = CheckFullName(request.FullName);
        var roleIds = CheckRoleIds(request.RoleIds);

        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        await LockAsync(ct);
        var roleNames = await RoleNamesAsync(roleIds, ct);
        var actor = await ActorAsync(actorIdentityUserId, ct);

        var user = new IdentityUser { UserName = userName };
        var created = await users.CreateAsync(user, password);
        if (!created.Succeeded)
        {
            if (created.Errors.Any(e => e.Code == nameof(IdentityErrorDescriber.DuplicateUserName)))
                throw new AppException(ApiErrorCodes.Conflict, "Tên đăng nhập đã được dùng.");
            throw Invalid("Không tạo được tài khoản: " + string.Join("; ", created.Errors.Select(e => e.Description)));
        }
        var staff = new StaffAccount { IdentityUserId = user.Id, FullName = fullName, IsActive = true };
        db.Add(staff);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException ex) when (IsDuplicate(ex)) { throw new AppException(ApiErrorCodes.Conflict, "Tên đăng nhập đã được dùng."); }
        foreach (var roleId in roleIds) db.Add(new StaffRole { StaffId = staff.StaffId, RoleId = roleId });
        Audit(actor.StaffId, "STAFF_CREATE", userName, "Họ tên: " + fullName + "; vai trò: " + string.Join(", ", roleNames), utcNow);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct); // lỗi ở bất kỳ bước nào hoàn tác cả tài khoản Identity: không để lại nhân viên dở dang
        return staff.StaffId;
    }

    // ---------------------------------------------------------------- Sửa họ tên và gán vai trò

    public async Task UpdateAsync(string actorIdentityUserId, long staffId, UpdateStaffRequest request, DateTime utcNow, CancellationToken ct = default)
    {
        var fullName = CheckFullName(request?.FullName);
        var roleIds = CheckRoleIds(request!.RoleIds);

        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        await LockAsync(ct);
        var staff = await db.StaffAccount.SingleOrDefaultAsync(s => s.StaffId == staffId, ct)
            ?? throw new AppException(ApiErrorCodes.NotFound, "Không tìm thấy nhân viên.");
        var roleNames = await RoleNamesAsync(roleIds, ct);
        var actor = await ActorAsync(actorIdentityUserId, ct);
        var current = await db.StaffRole.Where(x => x.StaffId == staffId).ToListAsync(ct);

        var adminRoles = await AdminRoleIdsAsync(ct);
        var wasAdmin = staff.IsActive && current.Any(x => adminRoles.Contains(x.RoleId));
        var willBeAdmin = staff.IsActive && roleIds.Any(adminRoles.Contains);
        if (wasAdmin && !willBeAdmin) await EnsureAnotherAdminAsync(staffId, ct);

        var keep = roleIds.ToHashSet();
        db.RemoveRange(current.Where(x => !keep.Contains(x.RoleId)));
        var have = current.Select(x => x.RoleId).ToHashSet();
        foreach (var roleId in roleIds.Where(id => !have.Contains(id))) db.Add(new StaffRole { StaffId = staffId, RoleId = roleId });
        staff.FullName = fullName;
        var userName = (await users.FindByIdAsync(staff.IdentityUserId))?.UserName ?? staff.IdentityUserId;
        Audit(actor.StaffId, "STAFF_UPDATE", userName, "Họ tên: " + fullName + "; vai trò: " + string.Join(", ", roleNames), utcNow);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    // ---------------------------------------------------------------- Khóa và mở

    // active = false: khóa (nhân viên không đăng nhập được, phiên đang mở bị thu hồi ở yêu cầu kế tiếp).
    // active = true: mở lại, đồng thời gỡ khóa tạm do nhập sai mật khẩu của Identity. Gọi lại khi đã ở trạng thái đó là an toàn (không lỗi).
    public async Task SetActiveAsync(string actorIdentityUserId, long staffId, bool active, DateTime utcNow, CancellationToken ct = default)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        await LockAsync(ct);
        var staff = await db.StaffAccount.SingleOrDefaultAsync(s => s.StaffId == staffId, ct)
            ?? throw new AppException(ApiErrorCodes.NotFound, "Không tìm thấy nhân viên.");
        var actor = await ActorAsync(actorIdentityUserId, ct);
        var user = await users.FindByIdAsync(staff.IdentityUserId)
            ?? throw new AppException(ApiErrorCodes.NotFound, "Không tìm thấy tài khoản đăng nhập của nhân viên.");

        if (!active)
        {
            if (staff.IsActive)
            {
                if (staff.StaffId == actor.StaffId) throw new AppException(ApiErrorCodes.Conflict, "Không thể tự khóa tài khoản của chính mình.");
                var adminRoles = await AdminRoleIdsAsync(ct);
                if (await db.StaffRole.AnyAsync(x => x.StaffId == staffId && adminRoles.Contains(x.RoleId), ct))
                    await EnsureAnotherAdminAsync(staffId, ct);
                staff.IsActive = false;
                Audit(actor.StaffId, "STAFF_DEACTIVATE", user.UserName!, null, utcNow);
                await db.SaveChangesAsync(ct);
            }
        }
        else
        {
            var wasInactive = !staff.IsActive;
            staff.IsActive = true;
            await users.SetLockoutEndDateAsync(user, null);
            await users.ResetAccessFailedCountAsync(user);
            if (wasInactive) Audit(actor.StaffId, "STAFF_ACTIVATE", user.UserName!, null, utcNow);
            await db.SaveChangesAsync(ct);
        }
        await tx.CommitAsync(ct);
    }

    // ---------------------------------------------------------------- Hỗ trợ

    private async Task<StaffAccount> ActorAsync(string identityUserId, CancellationToken ct) =>
        await db.StaffAccount.AsNoTracking().SingleOrDefaultAsync(s => s.IdentityUserId == identityUserId && s.IsActive, ct)
        ?? throw new AppException(ApiErrorCodes.Forbidden, "Tài khoản này không phải nhân viên đang hoạt động.");

    private async Task<List<string>> RoleNamesAsync(List<long> roleIds, CancellationToken ct)
    {
        var names = await db.Role.AsNoTracking().Where(r => roleIds.Contains(r.RoleId)).OrderBy(r => r.RoleName).Select(r => r.RoleName).ToListAsync(ct);
        if (names.Count != roleIds.Count) throw Invalid("Có vai trò không tồn tại. Hãy tải lại danh sách vai trò.");
        return names;
    }

    private async Task<HashSet<long>> AdminRoleIdsAsync(CancellationToken ct) =>
        (await (from rp in db.RolePermission.AsNoTracking()
                join p in db.Permission.AsNoTracking() on rp.PermissionId equals p.PermissionId
                where p.PermissionCode == Permissions.StaffManage
                select rp.RoleId).ToListAsync(ct)).ToHashSet();

    // Phải còn ít nhất một người khác đang hoạt động và có quyền STAFF_MANAGE, nếu không không ai còn quản lý được nhân viên.
    private async Task EnsureAnotherAdminAsync(long excludingStaffId, CancellationToken ct)
    {
        var other = await (from s in db.StaffAccount
                           join sr in db.StaffRole on s.StaffId equals sr.StaffId
                           join rp in db.RolePermission on sr.RoleId equals rp.RoleId
                           join p in db.Permission on rp.PermissionId equals p.PermissionId
                           where s.IsActive && s.StaffId != excludingStaffId && p.PermissionCode == Permissions.StaffManage
                           select s.StaffId).AnyAsync(ct);
        if (!other)
            throw new AppException(ApiErrorCodes.Conflict,
                "Hệ thống phải còn ít nhất một nhân viên đang hoạt động có quyền quản lý nhân viên, nên không thực hiện được thao tác này.");
    }

    private void Audit(long actorStaffId, string action, string userName, string? reason, DateTime utcNow) =>
        db.Add(new AuditEvent
        {
            StaffId = actorStaffId, ActionCode = action, EntityName = "StaffAccount", EntityReference = userName,
            Reason = reason is { Length: > 500 } ? reason[..500] : reason, CreatedAt = utcNow,
        });
}
