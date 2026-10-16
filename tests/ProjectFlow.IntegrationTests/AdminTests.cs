using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using ProjectFlow.Core.DTOs;
using ProjectFlow.Core.Enums;
using ProjectFlow.Infrastructure.Data;

namespace ProjectFlow.IntegrationTests;

public sealed class AdminTests
{
    [Fact]
    public async Task AdminEndpointsProtectRolesLastAdministratorAndActiveSessions()
    {
        using var app = new WorkspaceFactory(); await app.InitializeAsync(); using var admin = app.Browser(); using var member = app.Browser();
        await WorkspaceFactory.RegisterAsync(admin, "admin@example.test"); await WorkspaceFactory.RegisterAsync(member, "member@example.test");
        await PromoteAsync(app, "admin@example.test");
        await WorkspaceFactory.LoginAsync(admin, "admin@example.test"); await WorkspaceFactory.LoginAsync(member, "member@example.test");
        Assert.Equal(HttpStatusCode.Forbidden, (await member.GetAsync("/api/v1/admin/users")).StatusCode);
        var list = (await admin.GetFromJsonAsync<PageDto<AdminUserDto>>("/api/v1/admin/users"))!;
        var self = list.Items.Single(x => x.Email == "admin@example.test"); var other = list.Items.Single(x => x.Email == "member@example.test");
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PutAsJsonAsync($"/api/v1/admin/users/{self.Id}", new AdminUserRequest { ConcurrencyStamp = self.ConcurrencyStamp, Role = Roles.Member, IsActive = true })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PutAsJsonAsync($"/api/v1/admin/users/{self.Id}", new AdminUserRequest { ConcurrencyStamp = self.ConcurrencyStamp, Role = Roles.Admin, IsActive = false })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await member.PutAsJsonAsync($"/api/v1/admin/users/{other.Id}", new AdminUserRequest { ConcurrencyStamp = other.ConcurrencyStamp, Role = Roles.Admin, IsActive = true })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/Admin")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.PutAsJsonAsync($"/api/v1/admin/users/{other.Id}", new AdminUserRequest { ConcurrencyStamp = other.ConcurrencyStamp, Role = Roles.Manager, IsActive = false })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await member.GetAsync("/api/v1/account/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PutAsJsonAsync($"/api/v1/admin/users/{other.Id}", new AdminUserRequest { ConcurrencyStamp = other.ConcurrencyStamp, Role = Roles.Member, IsActive = true })).StatusCode);
    }

    [SqlServerFact]
    public async Task ParallelDemotionsCannotRemoveEveryActiveAdministrator()
    {
        using var app = new WorkspaceFactory(); await app.InitializeAsync(); using var a = app.Browser(); using var b = app.Browser();
        await WorkspaceFactory.RegisterAsync(a, "a@example.test"); await WorkspaceFactory.RegisterAsync(b, "b@example.test");
        await PromoteAsync(app, "a@example.test"); await PromoteAsync(app, "b@example.test");
        await WorkspaceFactory.LoginAsync(a, "a@example.test"); await WorkspaceFactory.LoginAsync(b, "b@example.test");
        var rows = (await a.GetFromJsonAsync<PageDto<AdminUserDto>>("/api/v1/admin/users"))!.Items;
        var ua = rows.Single(x => x.Email == "a@example.test"); var ub = rows.Single(x => x.Email == "b@example.test");
        var results = await Task.WhenAll(a.PutAsJsonAsync($"/api/v1/admin/users/{ua.Id}", new AdminUserRequest { ConcurrencyStamp = ua.ConcurrencyStamp, Role = Roles.Member, IsActive = true }), b.PutAsJsonAsync($"/api/v1/admin/users/{ub.Id}", new AdminUserRequest { ConcurrencyStamp = ub.ConcurrencyStamp, Role = Roles.Member, IsActive = true }));
        Assert.Single(results, r => r.StatusCode == HttpStatusCode.NoContent); Assert.Single(results, r => r.StatusCode == HttpStatusCode.Conflict);
    }
    private static async Task PromoteAsync(WorkspaceFactory app, string email)
    {
        using var scope = app.Services.CreateScope(); var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        Assert.True((await users.AddToRoleAsync((await users.FindByEmailAsync(email))!, Roles.Admin)).Succeeded);
    }
}
