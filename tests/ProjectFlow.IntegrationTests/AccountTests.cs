using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using ProjectFlow.Core.DTOs;
using ProjectFlow.Core.Enums;
using ProjectFlow.Infrastructure.Data;

namespace ProjectFlow.IntegrationTests;

public sealed class AccountTests
{
    [Fact]
    public async Task RegistrationLoginProfileLogoutAndCsrfAreEnforced()
    {
        using var app = new WorkspaceFactory();
        await app.InitializeAsync();
        using var c = app.Browser();
        Assert.Contains("action=\"/Account/Login\"", await c.GetStringAsync("/Account/Login"));
        Assert.Contains("action=\"/Account/Register\"", await c.GetStringAsync("/Account/Register"));
        var anonymous = await c.GetAsync("/api/v1/account/me");
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        Assert.Contains("application/problem+json", anonymous.Content.Headers.ContentType!.ToString());
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsJsonAsync("/api/v1/account/register", new
        {
        })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsJsonAsync("/api/v1/account/login", new
        {
            email = "member@example.test",
            password = "Test-only!Secret42"
        })).StatusCode);
        await WorkspaceFactory.RegisterAsync(c, "member@example.test", "<script>Sample</script>");
        await WorkspaceFactory.LoginAsync(c, "member@example.test");
        var missing = await c.GetAsync("/api/v1/unknown-resource");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Contains("application/problem+json", missing.Content.Headers.ContentType!.ToString());
        var me = await c.GetFromJsonAsync<AccountDto>("/api/v1/account/me");
        Assert.Equal([Roles.Member], me!.Roles);
        Assert.DoesNotContain(Roles.Admin, me.Roles);
        var profile = await c.GetStringAsync("/Account/Profile");
        Assert.Contains("&lt;script&gt;Sample&lt;/script&gt;", profile);
        Assert.DoesNotContain("<script>Sample", profile);
        Assert.Equal(HttpStatusCode.NoContent, (await c.PutAsJsonAsync("/api/v1/account/profile", new
        {
            displayName = "Renamed Member"
        })).StatusCode);
        Assert.Equal("Renamed Member", (await c.GetFromJsonAsync<AccountDto>("/api/v1/account/me"))!.DisplayName);
        Assert.Equal(HttpStatusCode.NoContent, (await c.PostAsync("/api/v1/account/logout", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.GetAsync("/api/v1/account/me")).StatusCode);
    }

    [Fact]
    public async Task LockoutInactiveSessionsAndRateLimitsCannotBeBypassedByForwardedHeaders()
    {
        using var app = new WorkspaceFactory();
        await app.InitializeAsync();
        using var c = app.Browser();
        await WorkspaceFactory.RegisterAsync(c, "locked@example.test");
        for (var i = 0; i < 5; i++)
            Assert.Equal(HttpStatusCode.Unauthorized, (await c.PostAsJsonAsync("/api/v1/account/login", new
            {
                email = "locked@example.test",
                password = "incorrect"
            })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.PostAsJsonAsync("/api/v1/account/login", new
        {
            email = "locked@example.test",
            password = "Test-only!Secret42"
        })).StatusCode);
        using (var scope = app.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = (await users.FindByEmailAsync("locked@example.test"))!;
            Assert.True(await users.IsLockedOutAsync(user));
            Assert.True((await users.SetLockoutEndDateAsync(user, null)).Succeeded);
        }
        await WorkspaceFactory.LoginAsync(c, "locked@example.test");
        using (var scope = app.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = (await users.FindByEmailAsync("locked@example.test"))!;
            user.IsActive = false;
            Assert.True((await users.UpdateAsync(user)).Succeeded);
        }
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.GetAsync("/api/v1/account/me")).StatusCode);
        await WorkspaceFactory.TokenAsync(c);
        HttpStatusCode status = 0;
        for (var i = 0; i < 25; i++)
        {
            c.DefaultRequestHeaders.Remove("X-Forwarded-For");
            c.DefaultRequestHeaders.Add("X-Forwarded-For", $"192.0.2.{i}");
            status = (await c.PostAsJsonAsync("/api/v1/account/login", new
            {
                email = "unknown@example.test",
                password = "incorrect"
            })).StatusCode;
        }
        Assert.Equal(HttpStatusCode.TooManyRequests, status);
    }
}
