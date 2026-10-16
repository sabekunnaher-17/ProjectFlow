using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ProjectFlow.Core.DTOs;
using ProjectFlow.Core.Enums;

namespace ProjectFlow.IntegrationTests;

public sealed class InsightTests
{
    [Fact]
    public async Task DashboardCommentsActivityAndCsvUseRealScopedData()
    {
        using var app = new WorkspaceFactory();
        await app.InitializeAsync();
        using var c = app.Browser();
        using var other = app.Browser();
        await WorkspaceFactory.RegisterAsync(c, "insight@example.test");
        await WorkspaceFactory.RegisterAsync(other, "outsider@example.test");
        await WorkspaceFactory.LoginAsync(c, "insight@example.test");
        await WorkspaceFactory.LoginAsync(other, "outsider@example.test");
        var userId = (await c.GetFromJsonAsync<AccountDto>("/api/v1/account/me"))!.Id;
        var orgId = await IdAsync(await c.PostAsJsonAsync("/api/v1/organizations", new
        {
            name = "Insight workspace"
        }));
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var projectId = await IdAsync(await c.PostAsJsonAsync("/api/v1/projects", new ProjectRequest { OrganizationId = orgId, Name = "Insight project", ManagerId = userId, StartDate = today.AddDays(-10), EndDate = today.AddDays(30), Status = ProjectStatus.Active }));
        var taskId = await IdAsync(await c.PostAsJsonAsync("/api/v1/tasks", new TaskRequest { ProjectId = projectId, Title = "=HYPERLINK(unsafe)", AssigneeId = userId, StartDate = today, DueDate = today.AddDays(2) }));
        var overdueId = await IdAsync(await c.PostAsJsonAsync("/api/v1/tasks", new TaskRequest { ProjectId = projectId, Title = "Overdue work", StartDate = today.AddDays(-5), DueDate = today.AddDays(-1) }));
        Assert.NotEqual(taskId, overdueId);
        Assert.Equal(HttpStatusCode.Created, (await c.PostAsJsonAsync($"/api/v1/tasks/{taskId}/comments", new
        {
            body = "<img src=x onerror=alert(1)>"
        })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await other.PostAsJsonAsync($"/api/v1/tasks/{taskId}/comments", new
        {
            body = "Forbidden comment"
        })).StatusCode);
        var comments = (await c.GetFromJsonAsync<PageDto<CommentDto>>($"/api/v1/tasks/{taskId}/comments"))!;
        Assert.Single(comments.Items);
        var html = await c.GetStringAsync($"/Tasks/Details/{taskId}");
        Assert.Contains("&lt;img", html);
        Assert.DoesNotContain("<img src=x", html);
        var dashboard = (await c.GetFromJsonAsync<DashboardDto>("/api/v1/dashboard"))!;
        Assert.Equal(1, dashboard.TotalProjects);
        Assert.Equal(1, dashboard.ActiveProjects);
        Assert.Equal(2, dashboard.TotalTasks);
        Assert.Equal(1, dashboard.OverdueTasks);
        Assert.Single(dashboard.Upcoming);
        Assert.Contains(dashboard.Recent, x => x.Description == "Task comment added.");
        Assert.Equal(dashboard.Recent.Select(x => x.CreatedAt).OrderByDescending(x => x), dashboard.Recent.Select(x => x.CreatedAt));
        Assert.Equal(0, (await other.GetFromJsonAsync<DashboardDto>("/api/v1/dashboard"))!.TotalTasks);
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync("/Dashboard")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync("/Dashboard/Activity")).StatusCode);
        var export = await c.GetStringAsync($"/api/v1/reports/tasks.csv?ProjectId={projectId}");
        Assert.Contains("\"'=HYPERLINK(unsafe)\"", export);
        Assert.Contains("Overdue work", export);
        Assert.DoesNotContain("Overdue work", await other.GetStringAsync("/api/v1/reports/tasks.csv"));
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync("/Reports")).StatusCode);
        var p = (await c.GetFromJsonAsync<ProjectDto>($"/api/v1/projects/{projectId}"))!;
        Assert.Equal(HttpStatusCode.NoContent, (await c.PostAsJsonAsync($"/api/v1/projects/{projectId}/archive", new
        {
            version = p.Version
        })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await c.PostAsJsonAsync($"/api/v1/tasks/{taskId}/comments", new
        {
            body = "Late edit"
        })).StatusCode);
    }
    private static async Task<Guid> IdAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }
}
