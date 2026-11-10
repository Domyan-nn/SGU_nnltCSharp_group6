using System.Security.Cryptography;
using CafeManager.Core;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CafeManager.Infrastructure;

// Password is null when the account already existed (never reset, never stored or logged).
public sealed record SeededAccount(string UserName, string Kind, string? Password);

public static class DevelopmentAccountSeeder
{
    // Opt-in, Development only (the host guards environment and database name).
    // Creates business roles/permissions and one account per kind with a random password returned to the caller once.
    // Existing accounts are never modified. Everything runs in one transaction: a failure leaves no half-created account.
    public static async Task<IReadOnlyList<SeededAccount>> SeedAsync(CafeDbContext db, UserManager<IdentityUser> users, CancellationToken ct = default)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var manager = await RoleAsync(db, "Manager", [Permissions.MenuManage], ct);
        var cashier = await RoleAsync(db, "Cashier", [Permissions.OrderReceive], ct);
        var result = new List<SeededAccount>
        {
            await StaffAsync(db, users, "dev.manager", "Quản lý thử", manager, ct),
            await StaffAsync(db, users, "dev.cashier", "Thu ngân thử", cashier, ct),
            await CustomerAsync(db, users, "dev.customer", "Khách thử", "0900000000", ct),
        };
        await tx.CommitAsync(ct);
        return result;
    }

    private static async Task<long> RoleAsync(CafeDbContext db, string roleName, string[] permissionCodes, CancellationToken ct)
    {
        var role = await db.Role.SingleOrDefaultAsync(x => x.RoleName == roleName, ct);
        if (role == null) { role = new Role { RoleName = roleName }; db.Add(role); await db.SaveChangesAsync(ct); }
        foreach (var code in permissionCodes)
        {
            var permission = await db.Permission.SingleOrDefaultAsync(x => x.PermissionCode == code, ct);
            if (permission == null) { permission = new Permission { PermissionCode = code }; db.Add(permission); await db.SaveChangesAsync(ct); }
            if (!await db.RolePermission.AnyAsync(x => x.RoleId == role.RoleId && x.PermissionId == permission.PermissionId, ct))
            { db.Add(new RolePermission { RoleId = role.RoleId, PermissionId = permission.PermissionId }); await db.SaveChangesAsync(ct); }
        }
        return role.RoleId;
    }

    private static async Task<(IdentityUser User, string? Password)> UserAsync(UserManager<IdentityUser> users, string userName)
    {
        var user = await users.FindByNameAsync(userName);
        if (user != null) return (user, null);
        user = new IdentityUser { UserName = userName };
        var password = NewPassword();
        var created = await users.CreateAsync(user, password);
        if (!created.Succeeded) throw new InvalidOperationException("Cannot create " + userName + ": " + string.Join("; ", created.Errors.Select(e => e.Code)));
        return (user, password);
    }

    private static async Task<SeededAccount> StaffAsync(CafeDbContext db, UserManager<IdentityUser> users, string userName, string fullName, long roleId, CancellationToken ct)
    {
        var (user, password) = await UserAsync(users, userName);
        var staff = await db.StaffAccount.SingleOrDefaultAsync(x => x.IdentityUserId == user.Id, ct);
        if (staff == null) { staff = new StaffAccount { IdentityUserId = user.Id, FullName = fullName, IsActive = true }; db.Add(staff); await db.SaveChangesAsync(ct); }
        if (!await db.StaffRole.AnyAsync(x => x.StaffId == staff.StaffId && x.RoleId == roleId, ct))
        { db.Add(new StaffRole { StaffId = staff.StaffId, RoleId = roleId }); await db.SaveChangesAsync(ct); }
        return new SeededAccount(userName, UserKinds.Staff, password);
    }

    private static async Task<SeededAccount> CustomerAsync(CafeDbContext db, UserManager<IdentityUser> users, string userName, string fullName, string phone, CancellationToken ct)
    {
        var (user, password) = await UserAsync(users, userName);
        if (!await db.CustomerProfile.AnyAsync(x => x.IdentityUserId == user.Id, ct))
        { db.Add(new CustomerProfile { IdentityUserId = user.Id, FullName = fullName, Phone = phone }); await db.SaveChangesAsync(ct); }
        return new SeededAccount(userName, UserKinds.Customer, password);
    }

    // 16 ký tự, đủ chữ hoa, chữ thường, chữ số và ký hiệu để thỏa chính sách Identity mặc định.
    private static string NewPassword()
    {
        const string lower = "abcdefghijkmnopqrstuvwxyz", upper = "ABCDEFGHJKLMNPQRSTUVWXYZ", digits = "23456789", symbols = "!@#$%*?";
        var chars = new List<char> { Pick(lower), Pick(upper), Pick(digits), Pick(symbols) };
        while (chars.Count < 16) chars.Add(Pick(lower + upper + digits));
        var array = chars.ToArray();
        RandomNumberGenerator.Shuffle(array.AsSpan());
        return new string(array);
    }

    private static char Pick(string set) => set[RandomNumberGenerator.GetInt32(set.Length)];
}
