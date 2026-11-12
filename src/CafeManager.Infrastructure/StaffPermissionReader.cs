using Microsoft.EntityFrameworkCore;
namespace CafeManager.Infrastructure;
public sealed class StaffPermissionReader(CafeDbContext db)
{
    public Task<bool> HasAsync(string identityUserId, string permissionCode, CancellationToken ct = default) =>
        (from staff in db.StaffAccount
         join sr in db.StaffRole on staff.StaffId equals sr.StaffId
         join rp in db.RolePermission on sr.RoleId equals rp.RoleId
         join p in db.Permission on rp.PermissionId equals p.PermissionId
         where staff.IdentityUserId == identityUserId && staff.IsActive && p.PermissionCode == permissionCode
         select p.PermissionId).AnyAsync(ct);

    public async Task<IReadOnlyList<string>> ListAsync(string identityUserId, CancellationToken ct = default) =>
        await (from staff in db.StaffAccount
               join sr in db.StaffRole on staff.StaffId equals sr.StaffId
               join rp in db.RolePermission on sr.RoleId equals rp.RoleId
               join p in db.Permission on rp.PermissionId equals p.PermissionId
               where staff.IdentityUserId == identityUserId && staff.IsActive
               select p.PermissionCode).Distinct().OrderBy(x => x).ToListAsync(ct);
}

