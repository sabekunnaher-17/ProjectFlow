using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace ProjectFlow.Web.Security;

public static class ApiCookieResponses
{
    public static Task RedirectAsync(RedirectContext<CookieAuthenticationOptions> context, int status)
    {
        if (context.Request.Path.StartsWithSegments("/api"))
            return Results.Problem(statusCode: status, title: status == 401 ? "Sign in to continue." : "You do not have permission for this operation.").ExecuteAsync(context.HttpContext);
        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    }
}
