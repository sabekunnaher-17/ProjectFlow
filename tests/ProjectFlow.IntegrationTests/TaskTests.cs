using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ProjectFlow.Core.DTOs;
using ProjectFlow.Core.Enums;
using TaskState = ProjectFlow.Core.Enums.TaskStatus;

namespace ProjectFlow.IntegrationTests;

public sealed class TaskTests
{
    [Fact]
    public async Task TaskJourneyEnforcesAssignmentDatesTransitionsScopeFilteringAndConcurrency()
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
        var outsiderId = (await outsider.GetFromJsonAsync<AccountDto>("/api/v1/account/me"))!.Id;
        var orgId = await IdAsync(await owner.PostAsJsonAsync("/api/v1/organizations", new
        {
            name = "Task workspace"
        }));
        Assert.Equal(HttpStatusCode.NoContent, (await owner.PostAsJsonAsync($"/api/v1/organizations/{orgId}/members", new
        {
            email = "member@example.test"
        })).StatusCode);
        var projectId = await IdAsync(await owner.PostAsJsonAsync("/api/v1/projects", new ProjectRequest { OrganizationId = orgId, Name = "Task project", ManagerId = ownerId, StartDate = new(2026, 1, 1), EndDate = new(2026, 12, 31) }));
        var p = (await owner.GetFromJsonAsync<ProjectDto>($"/api/v1/projects/{projectId}"))!;
        Assert.Equal(HttpStatusCode.NoContent, (await owner.PostAsJsonAsync($"/api/v1/projects/{projectId}/members", new
        {
            userId = memberId,
            version = p.Version
        })).StatusCode);
        var input = new TaskRequest { ProjectId = projectId, Title = "Validated task", AssigneeId = outsiderId, StartDate = new(2026, 1, 2), DueDate = new(2026, 2, 1), Priority = TaskPriority.High };
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.PostAsJsonAsync("/api/v1/tasks", input)).StatusCode);
        input.AssigneeId = memberId;
        input.DueDate = new(2027, 1, 1);
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.PostAsJsonAsync("/api/v1/tasks", input)).StatusCode);
        input.DueDate = new(2026, 2, 1);
        Assert.Equal(HttpStatusCode.Forbidden, (await member.PostAsJsonAsync("/api/v1/tasks", input)).StatusCode);
        var id = await IdAsync(await owner.PostAsJsonAsync("/api/v1/tasks", input));
        Assert.Equal(HttpStatusCode.NotFound, (await outsider.GetAsync($"/api/v1/tasks/{id}")).StatusCode);
        var task = (await owner.GetFromJsonAsync<TaskDto>($"/api/v1/tasks/{id}"))!;
        input.Version = task.Version;
        Assert.Equal(HttpStatusCode.Forbidden, (await member.PutAsJsonAsync($"/api/v1/tasks/{id}", input)).StatusCode);
        var editHtml = await owner.GetStringAsync($"/Tasks/Edit/{id}");
        Assert.Contains($"action=\"/Tasks/Edit/{id}\"", editHtml);
        Assert.Contains("value=\"Validated task\"", editHtml);
        Assert.Equal(HttpStatusCode.BadRequest, (await member.PutAsJsonAsync($"/api/v1/tasks/{id}/status", new
        {
            status = TaskState.Completed,
            version = task.Version
        })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await member.PutAsJsonAsync($"/api/v1/tasks/{id}/status", new
        {
            status = TaskState.InProgress,
            version = task.Version
        })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await owner.PutAsJsonAsync($"/api/v1/tasks/{id}", input)).StatusCode);
        foreach (var status in new[] { TaskState.InReview, TaskState.Completed })
        {
            task = (await member.GetFromJsonAsync<TaskDto>($"/api/v1/tasks/{id}"))!;
            Assert.Equal(HttpStatusCode.NoContent, (await member.PutAsJsonAsync($"/api/v1/tasks/{id}/status", new
            {
                status,
                version = task.Version
            })).StatusCode);
        }
        task = (await member.GetFromJsonAsync<TaskDto>($"/api/v1/tasks/{id}"))!;
        Assert.NotNull(task.CompletedAt);
        Assert.Equal(HttpStatusCode.BadRequest, (await member.PutAsJsonAsync($"/api/v1/tasks/{id}/status", new
        {
            status = TaskState.InProgress,
            version = task.Version
        })).StatusCode);
        Assert.Equal(100, (await owner.GetFromJsonAsync<ProjectDto>($"/api/v1/projects/{projectId}"))!.Progress);
        var mine = (await member.GetFromJsonAsync<PageDto<TaskDto>>("/api/v1/tasks?Mine=true&Status=Completed&Priority=High&Sort=priority&PageSize=1"))!;
        Assert.Equal(1, mine.Total);
        Assert.Single(mine.Items);
        Assert.Empty((await outsider.GetFromJsonAsync<PageDto<TaskDto>>("/api/v1/tasks"))!.Items);
        Assert.Contains("Validated task", await member.GetStringAsync($"/Tasks?projectId={projectId}&board=true"));
        p = (await owner.GetFromJsonAsync<ProjectDto>($"/api/v1/projects/{projectId}"))!;
        using var remove = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/projects/{projectId}/members/{memberId}") { Content = JsonContent.Create(new { version = p.Version }) };
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.SendAsync(remove)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await owner.PostAsJsonAsync($"/api/v1/projects/{projectId}/archive", new
        {
            version = p.Version
        })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await owner.PutAsJsonAsync($"/api/v1/tasks/{id}/status", new
        {
            status = TaskState.InProgress,
            version = task.Version
        })).StatusCode);
    }

    [SqlServerFact]
    public async Task ParallelStatusUpdatesAcceptExactlyOneWriter()
    {
        using var app = new WorkspaceFactory();
        await app.InitializeAsync();
        using var c = app.Browser();
        await WorkspaceFactory.RegisterAsync(c, "parallel@example.test");
        await WorkspaceFactory.LoginAsync(c, "parallel@example.test");
        var userId = (await c.GetFromJsonAsync<AccountDto>("/api/v1/account/me"))!.Id;
        var orgId = await IdAsync(await c.PostAsJsonAsync("/api/v1/organizations", new
        {
            name = "Concurrent workspace"
        }));
        var pId = await IdAsync(await c.PostAsJsonAsync("/api/v1/projects", new ProjectRequest { OrganizationId = orgId, Name = "Concurrent project", ManagerId = userId, StartDate = new(2026, 1, 1), EndDate = new(2026, 12, 31) }));
        var id = await IdAsync(await c.PostAsJsonAsync("/api/v1/tasks", new TaskRequest { ProjectId = pId, Title = "Concurrent task", StartDate = new(2026, 1, 1), DueDate = new(2026, 12, 1) }));
        var task = (await c.GetFromJsonAsync<TaskDto>($"/api/v1/tasks/{id}"))!;
        var responses = await Task.WhenAll(c.PutAsJsonAsync($"/api/v1/tasks/{id}/status", new
        {
            status = TaskState.InProgress,
            version = task.Version
        }), c.PutAsJsonAsync($"/api/v1/tasks/{id}/status", new
        {
            status = TaskState.Cancelled,
            version = task.Version
        }));
        Assert.Single(responses, x => x.StatusCode == HttpStatusCode.NoContent);
        Assert.Single(responses, x => x.StatusCode == HttpStatusCode.Conflict);
    }
    private static async Task<Guid> IdAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }
}

public sealed class SqlServerFactAttribute : FactAttribute
{
    public SqlServerFactAttribute()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PROJECTFLOW_TEST_SQL_SERVER")))
            Skip = "Requires isolated SQL Server fixture; local SQLite cannot verify concurrent SQL writes.";
    }
}
