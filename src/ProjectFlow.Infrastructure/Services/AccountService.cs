using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using ProjectFlow.Core.Common;
using ProjectFlow.Core.DTOs;
using ProjectFlow.Core.Enums;
using ProjectFlow.Infrastructure.Data;

namespace ProjectFlow.Infrastructure.Services;

public sealed class AccountService(UserManager<ApplicationUser> users, RoleManager<IdentityRole> roles,
    SignInManager<ApplicationUser> signIn, ApplicationDbContext db, UnknownPasswordVerifier unknown)
{
    public async Task EnsureRolesAsync()
    {
        foreach (var name in Roles.All)
        {
            if (await roles.RoleExistsAsync(name))
                continue;
            var result = await roles.CreateAsync(new IdentityRole(name));
            if (!result.Succeeded && !await roles.RoleExistsAsync(name))
                throw new InvalidOperationException("Unable to initialize application roles.");
        }
    }

    public async Task RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        await EnsureRolesAsync();
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var user = new ApplicationUser { UserName = request.Email.Trim(), Email = request.Email.Trim(), DisplayName = request.DisplayName.Trim() };
        var result = await users.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            throw new DomainException("Registration could not be completed. Check your details or try signing in.");
        var assigned = await users.AddToRoleAsync(user, Roles.Member);
        if (!assigned.Succeeded)
            throw new InvalidOperationException("Unable to assign the default role.");
        await transaction.CommitAsync(ct);
        // Explicit login follows registration; a posted role/admin field is never bound.
    }

    public async Task<bool> LoginAsync(LoginRequest request)
    {
        var user = await users.FindByEmailAsync(request.Email.Trim());
        if (user is null || !user.IsActive)
        {
            unknown.Verify(request.Password);
            return false;
        }
        return (await signIn.PasswordSignInAsync(user, request.Password, false, lockoutOnFailure: true)).Succeeded;
    }

    public Task LogoutAsync() => signIn.SignOutAsync();

    public async Task<AccountDto> GetAsync(ClaimsPrincipal principal)
    {
        var user = await users.GetUserAsync(principal);
        if (user is null || !user.IsActive)
            throw new DomainException("Sign in to continue.", FailureKind.Forbidden);
        return new(user.Id, user.DisplayName, user.Email ?? "", (await users.GetRolesAsync(user)).ToArray());
    }

    public async Task UpdateAsync(ClaimsPrincipal principal, ProfileRequest request)
    {
        var user = await users.GetUserAsync(principal);
        if (user is null || !user.IsActive)
            throw new DomainException("Sign in to continue.", FailureKind.Forbidden);
        user.DisplayName = request.DisplayName.Trim();
        if (!(await users.UpdateAsync(user)).Succeeded)
            throw new DomainException("Profile update could not be saved.", FailureKind.Conflict);
    }
}
