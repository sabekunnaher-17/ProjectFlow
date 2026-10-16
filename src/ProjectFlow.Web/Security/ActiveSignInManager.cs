using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using ProjectFlow.Infrastructure.Data;

namespace ProjectFlow.Web.Security;

public sealed class ActiveSignInManager(UserManager<ApplicationUser> users, IHttpContextAccessor context,
    IUserClaimsPrincipalFactory<ApplicationUser> claims, IOptions<IdentityOptions> options,
    ILogger<SignInManager<ApplicationUser>> logger, IAuthenticationSchemeProvider schemes,
    IUserConfirmation<ApplicationUser> confirmation)
    : SignInManager<ApplicationUser>(users, context, claims, options, logger, schemes, confirmation)
{
    public override async Task<bool> CanSignInAsync(ApplicationUser user) => user.IsActive && await base.CanSignInAsync(user);
    public override async Task<ApplicationUser?> ValidateSecurityStampAsync(System.Security.Claims.ClaimsPrincipal? principal)
    {
        var user = await base.ValidateSecurityStampAsync(principal);
        return user?.IsActive == true ? user : null;
    }
}
