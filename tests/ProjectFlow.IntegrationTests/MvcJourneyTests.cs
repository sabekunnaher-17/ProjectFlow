using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using ProjectFlow.Core.DTOs;

namespace ProjectFlow.IntegrationTests;

public sealed class MvcJourneyTests
{
    [Fact]
    public async Task OptionalDescriptionsRemainOptionalInRealFormPosts()
    {
        using var app = new WorkspaceFactory();
        await app.InitializeAsync();
        using var c = app.Browser();
        await WorkspaceFactory.RegisterAsync(c, "mvc@example.test");
        await WorkspaceFactory.LoginAsync(c, "mvc@example.test");
        var user = (await c.GetFromJsonAsync<AccountDto>("/api/v1/account/me"))!;
        var orgResponse = await c.PostAsJsonAsync("/api/v1/organizations", new
        {
            name = "MVC workspace"
        });
        var orgId = (await orgResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var token = await TokenAsync(c, $"/Projects/Create?organizationId={orgId}");
        c.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        var result = await c.PostAsync("/Projects/Create", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["OrganizationId"] = orgId.ToString(),
            ["Name"] = "MVC project",
            ["Description"] = "",
            ["ManagerId"] = user.Id,
            ["StartDate"] = "2026-01-01",
            ["EndDate"] = "2026-12-31"
        }));
        Assert.Equal(HttpStatusCode.Redirect, result.StatusCode);
        var id = Guid.Parse(result.Headers.Location!.ToString().Split('/').Last());
        token = await TokenAsync(c, $"/Tasks/Create?projectId={id}");
        result = await c.PostAsync("/Tasks/Create", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["ProjectId"] = id.ToString(),
            ["Title"] = "MVC task",
            ["Description"] = "",
            ["StartDate"] = "2026-01-01",
            ["DueDate"] = "2026-12-31"
        }));
        Assert.Equal(HttpStatusCode.Redirect, result.StatusCode);
    }
    private static async Task<string> TokenAsync(HttpClient c, string path)
    {
        var html = await c.GetStringAsync(path);
        var token = Regex.Match(html, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;
        Assert.NotEmpty(token);
        return token;
    }
}
