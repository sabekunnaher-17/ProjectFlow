using System.Security.Claims;
using ProjectFlow.Core.Enums;
using ProjectFlow.Core.Interfaces;

namespace ProjectFlow.Web.Security;

public sealed class HttpActor(IHttpContextAccessor context) : IActor
{
    private ClaimsPrincipal User => context.HttpContext?.User ?? new();
    public string Id => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("An authenticated actor is required.");
    public bool IsAdmin => User.IsInRole(Roles.Admin);
    public bool IsManager => User.IsInRole(Roles.Manager);
}
