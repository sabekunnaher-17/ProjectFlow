using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProjectFlow.Core.Common;
using ProjectFlow.Core.DTOs;
using ProjectFlow.Core.Enums;
using ProjectFlow.Core.Interfaces;
using ProjectFlow.Infrastructure.Data;

namespace ProjectFlow.Infrastructure.Services;

public sealed class AdminService(ApplicationDbContext db, UserManager<ApplicationUser> users, IActor actor)
{
    private void RequireAdmin()
    {
        if (!actor.IsAdmin)
            throw new DomainException("Administrator permission is required.", FailureKind.Forbidden);
    }
    public async Task<PageDto<AdminUserDto>> ListAsync(string? keyword, int page, CancellationToken ct)
    {
        RequireAdmin();
        var q = db.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(keyword))
            q = q.Where(x => x.DisplayName.Contains(keyword) || x.Email!.Contains(keyword));
        var total = await q.CountAsync(ct);
        var rows = new List<AdminUserDto>();
        foreach (var u in await q.OrderBy(x => x.DisplayName).ThenBy(x => x.Id).Skip((page - 1) * 20).Take(20).ToListAsync(ct))
            rows.Add(new(u.Id, u.DisplayName, u.Email ?? "", u.IsActive, u.ConcurrencyStamp!, (await users.GetRolesAsync(u)).ToArray()));
        return new(rows, total, page, 20);
    }
    public async Task UpdateAsync(string id, AdminUserRequest input, CancellationToken ct)
    {
        RequireAdmin();
        if (!Roles.All.Contains(input.Role))
            throw new DomainException("Choose a supported role.");
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await DatabaseGate.LockAsync(db, true, ct);
        var adminRole = Roles.Admin.ToUpperInvariant();
        var activeAdministrators = db.Users.Where(u => u.IsActive && db.UserRoles.Any(ur => ur.UserId == u.Id && db.Roles.Any(r => r.Id == ur.RoleId && r.NormalizedName == adminRole)));
        if (!await activeAdministrators.AnyAsync(u => u.Id == actor.Id, ct))
            throw new DomainException("Administrator permission is required.", FailureKind.Forbidden);
        var user = await users.FindByIdAsync(id) ?? throw new DomainException("User not found.", FailureKind.NotFound);
        await db.Entry(user).ReloadAsync(ct);
        if (user.ConcurrencyStamp != input.ConcurrencyStamp)
            throw new DomainException("This user changed. Reload before saving.", FailureKind.Conflict);
        var current = await users.GetRolesAsync(user);
        if (current.Contains(Roles.Admin) && user.IsActive && (!input.IsActive || input.Role != Roles.Admin))
        {
            var activeAdmins = await activeAdministrators.CountAsync(ct);
            if (activeAdmins <= 1)
                throw new DomainException("The last active administrator cannot be removed or deactivated.", FailureKind.Conflict);
        }
        Check(await users.RemoveFromRolesAsync(user, current));
        Check(await users.AddToRoleAsync(user, input.Role));
        user.IsActive = input.IsActive;
        Check(await users.UpdateAsync(user));
        Check(await users.UpdateSecurityStampAsync(user));
        await transaction.CommitAsync(ct);
    }
    private static void Check(IdentityResult result)
    {
        if (!result.Succeeded)
            throw new DomainException("User update could not be saved. Reload and try again.", FailureKind.Conflict);
    }
}
