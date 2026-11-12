using System.Text.RegularExpressions;
using CafeManager.Core;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CafeManager.Infrastructure.Services;

// Đăng ký và đăng nhập (UC18, UC19). Danh tính do ASP.NET Core Identity quản lý; quyền nghiệp vụ lấy từ
// StaffRole -> RolePermission. Mọi lỗi nghiệp vụ ném AppException với mã trong ApiErrorCodes.
public sealed partial class AccountService(CafeDbContext db, UserManager<IdentityUser> users, StaffPermissionReader permissions)
{
    public sealed record Session(string IdentityUserId, string UserName, string Kind, string DisplayName, IReadOnlyList<string> Permissions)
    {
        public LoginResponse ToResponse() => new(Kind, DisplayName, Permissions);
    }

    [GeneratedRegex(@"^[A-Za-z0-9._-]+$")] private static partial Regex UserNameRegex();
    [GeneratedRegex(@"^(0|\+84)\d{9}$")] private static partial Regex PhoneRegex();

    // preferredKind: Staff cho Desktop, Customer cho Web khách; null = nhân viên (nếu có) rồi đến khách.
    public async Task<Session> LoginAsync(LoginRequest request, string? preferredKind = null, CancellationToken ct = default)
    {
        const string wrong = "Tên đăng nhập hoặc mật khẩu không đúng.";
        if (string.IsNullOrWhiteSpace(request.UserName) || string.IsNullOrEmpty(request.Password))
            throw new AppException(ApiErrorCodes.Validation, "Nhập tên đăng nhập và mật khẩu.");
        var user = await users.FindByNameAsync(request.UserName.Trim());
        if (user == null) throw new AppException(ApiErrorCodes.Unauthorized, wrong);
        if (await users.IsLockedOutAsync(user)) throw new AppException(ApiErrorCodes.AccountLocked, "Tài khoản đã bị khóa.");
        if (!await users.CheckPasswordAsync(user, request.Password))
        {
            await users.AccessFailedAsync(user);
            if (await users.IsLockedOutAsync(user)) throw new AppException(ApiErrorCodes.AccountLocked, "Tài khoản đã bị khóa do nhập sai nhiều lần.");
            throw new AppException(ApiErrorCodes.Unauthorized, wrong);
        }
        await users.ResetAccessFailedCountAsync(user);
        return await DescribeAsync(user, preferredKind, ct);
    }

    // Dùng khi kiểm tra lại phiên ở mỗi yêu cầu: khóa tài khoản hoặc thu hồi nhân viên có hiệu lực ở yêu cầu kế tiếp.
    public async Task<Session?> RevalidateAsync(string identityUserId, string kind, CancellationToken ct = default)
    {
        var user = await users.FindByIdAsync(identityUserId);
        if (user == null || await users.IsLockedOutAsync(user)) return null;
        try { return await DescribeAsync(user, kind, ct); }
        catch (AppException) { return null; }
    }

    private async Task<Session> DescribeAsync(IdentityUser user, string? preferredKind, CancellationToken ct)
    {
        var staff = await db.StaffAccount.AsNoTracking().SingleOrDefaultAsync(x => x.IdentityUserId == user.Id, ct);
        var customer = await db.CustomerProfile.AsNoTracking().SingleOrDefaultAsync(x => x.IdentityUserId == user.Id, ct);
        var kind = preferredKind ?? (staff != null ? UserKinds.Staff : UserKinds.Customer);
        if (kind == UserKinds.Staff)
        {
            if (staff == null) throw new AppException(ApiErrorCodes.Forbidden, "Tài khoản này không phải tài khoản nhân viên.");
            if (!staff.IsActive) throw new AppException(ApiErrorCodes.AccountLocked, "Tài khoản nhân viên đã bị vô hiệu hóa.");
            return new(user.Id, user.UserName!, kind, staff.FullName, await permissions.ListAsync(user.Id, ct));
        }
        if (customer == null) throw new AppException(ApiErrorCodes.Forbidden, "Tài khoản này không phải tài khoản khách.");
        return new(user.Id, user.UserName!, UserKinds.Customer, customer.FullName, []);
    }

    public async Task RegisterCustomerAsync(RegisterCustomerRequest request, CancellationToken ct = default)
    {
        var userName = (request.UserName ?? "").Trim();
        var fullName = (request.FullName ?? "").Trim();
        if (userName.Length is < 3 or > 50 || !UserNameRegex().IsMatch(userName))
            throw new AppException(ApiErrorCodes.Validation, "Tên đăng nhập 3 đến 50 ký tự gồm chữ không dấu, số, dấu chấm, gạch dưới hoặc gạch nối.");
        if ((request.Password ?? "").Length < 6 || request.Password!.Length > 100)
            throw new AppException(ApiErrorCodes.Validation, "Mật khẩu từ 6 đến 100 ký tự.");
        if (fullName.Length is 0 or > 100) throw new AppException(ApiErrorCodes.Validation, "Nhập họ tên (tối đa 100 ký tự).");
        if (!PhoneRegex().IsMatch(request.Phone ?? "")) throw new AppException(ApiErrorCodes.Validation, "Số điện thoại không hợp lệ.");

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var user = new IdentityUser { UserName = userName };
        var created = await users.CreateAsync(user, request.Password);
        if (!created.Succeeded)
        {
            if (created.Errors.Any(e => e.Code == nameof(IdentityErrorDescriber.DuplicateUserName)))
                throw new AppException(ApiErrorCodes.Conflict, "Tên đăng nhập đã được dùng.");
            throw new AppException(ApiErrorCodes.Validation, "Không tạo được tài khoản: " + string.Join("; ", created.Errors.Select(e => e.Description)));
        }
        db.Add(new CustomerProfile { IdentityUserId = user.Id, FullName = fullName, Phone = request.Phone! });
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct); // lỗi ở bước hồ sơ làm hoàn tác cả tài khoản: không để lại dữ liệu dở dang
    }
}
