using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using ProjectFlow.Core.DTOs;

namespace ProjectFlow.IntegrationTests;

public sealed class ProjectTests
{
    [Fact]
    public async Task ProjectsEnforceOrganizationScopeMembershipConcurrencyAndArchive()
    {
        using var app = new WorkspaceFactory();
        await app.InitializeAsync();
        using var owner = app.Browser();
        using var member = app.Browser();
        using var outsider = app.Browser();
        await WorkspaceFactory.RegisterAsync(owner, "owner@example.test");
        await WorkspaceFactory.RegisterAsync(member, "member@example.test");
        await WorkspaceFactory.RegisterAsync(outsider, "outsider@example.test");
        await WorkspaceFactory.LoginAsync(owner, "owner@example.test");
        await WorkspaceFactory.LoginAsync(member, "member@example.test");
        await WorkspaceFactory.LoginAsync(outsider, "outsider@example.test");
        var ownerId = (await owner.GetFromJsonAsync<AccountDto>("/api/v1/account/me"))!.Id;
        var memberId = (await member.GetFromJsonAsync<AccountDto>("/api/v1/account/me"))!.Id;
        var orgResponse = await owner.PostAsJsonAsync("/api/v1/organizations", new
        {
            name = "Engineering"
        });
        Assert.Equal(HttpStatusCode.Created, orgResponse.StatusCode);
        var orgId = (await orgResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        Assert.Empty((await outsider.GetFromJsonAsync<OrganizationDto[]>("/api/v1/organizations"))!);
        Assert.Equal(HttpStatusCode.NoContent, (await owner.PostAsJsonAsync($"/api/v1/organizations/{orgId}/members", new
        {
            email = "member@example.test"
        })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await member.PostAsJsonAsync($"/api/v1/organizations/{orgId}/members", new
        {
            email = "outsider@example.test"
        })).StatusCode);
        var input = new ProjectRequest { OrganizationId = orgId, Name = "Reliable release", ManagerId = ownerId, StartDate = new(2026, 1, 1), EndDate = new(2026, 12, 31) };
        Assert.Equal(HttpStatusCode.Forbidden, (await member.PostAsJsonAsync("/api/v1/projects", input)).StatusCode);
        var created = await owner.PostAsJsonAsync("/api/v1/projects", input);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.NotFound, (await outsider.GetAsync($"/api/v1/projects/{id}")).StatusCode);
        var project = (await owner.GetFromJsonAsync<ProjectDto>($"/api/v1/projects/{id}"))!;
        Assert.Equal(HttpStatusCode.NoContent, (await owner.PostAsJsonAsync($"/api/v1/projects/{id}/members", new
        {
            userId = memberId,
            version = project.Version
        })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await member.GetAsync($"/api/v1/projects/{id}")).StatusCode);
        input.Version = project.Version;
        Assert.Equal(HttpStatusCode.Conflict, (await owner.PutAsJsonAsync($"/api/v1/projects/{id}", input)).StatusCode);
        project = (await owner.GetFromJsonAsync<ProjectDto>($"/api/v1/projects/{id}"))!;
        input.Version = project.Version;
        Assert.Equal(HttpStatusCode.Forbidden, (await member.PutAsJsonAsync($"/api/v1/projects/{id}", input)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await owner.PutAsJsonAsync($"/api/v1/projects/{id}", input)).StatusCode);
        project = (await owner.GetFromJsonAsync<ProjectDto>($"/api/v1/projects/{id}"))!;
        Assert.Equal(HttpStatusCode.NoContent, (await owner.PostAsJsonAsync($"/api/v1/projects/{id}/archive", new
        {
            version = project.Version
        })).StatusCode);
        project = (await owner.GetFromJsonAsync<ProjectDto>($"/api/v1/projects/{id}"))!;
        input.Version = project.Version;
        Assert.Equal(HttpStatusCode.Conflict, (await owner.PutAsJsonAsync($"/api/v1/projects/{id}", input)).StatusCode);
    }

    [Fact]
    public async Task ProjectEditFormAndValidationPreserveActionIdentityAndSubmittedValues()
    {
        using var app = new WorkspaceFactory();
        await app.InitializeAsync();
        using var c = app.Browser();
        await WorkspaceFactory.RegisterAsync(c, "form@example.test");
        await WorkspaceFactory.LoginAsync(c, "form@example.test");
        var user = (await c.GetFromJsonAsync<AccountDto>("/api/v1/account/me"))!;
        var createdOrg = await c.PostAsJsonAsync("/api/v1/organizations", new
        {
            name = "Forms workspace"
        });
        var orgId = (await createdOrg.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var input = new ProjectRequest { OrganizationId = orgId, Name = "Rendered project", ManagerId = user.Id, StartDate = new(2026, 1, 1), EndDate = new(2026, 12, 31) };
        var created = await c.PostAsJsonAsync("/api/v1/projects", input);
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var project = (await c.GetFromJsonAsync<ProjectDto>($"/api/v1/projects/{id}"))!;
        var html = await c.GetStringAsync($"/Projects/Edit/{id}");
        Assert.Contains($"action=\"/Projects/Edit/{id}\"", html);
        Assert.Contains("value=\"Rendered project\"", html);
        Assert.Contains($"value=\"{project.Version}\"", html);
        var token = Regex.Match(html, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;
        Assert.NotEmpty(token);
        c.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        var response = await c.PostAsync($"/Projects/Edit/{id}", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["OrganizationId"] = orgId.ToString(),
            ["Name"] = "Rejected dates",
            ["ManagerId"] = user.Id,
            ["StartDate"] = "2026-12-31",
            ["EndDate"] = "2026-01-01",
            ["Version"] = project.Version.ToString()
        }));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        html = await response.Content.ReadAsStringAsync();
        Assert.Contains("value=\"Rejected dates\"", html);
        Assert.Contains($"action=\"/Projects/Edit/{id}\"", html);
        Assert.Equal("Rendered project", (await c.GetFromJsonAsync<ProjectDto>($"/api/v1/projects/{id}"))!.Name);
    }
}
