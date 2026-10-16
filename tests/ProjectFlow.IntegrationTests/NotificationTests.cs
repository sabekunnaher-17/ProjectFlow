using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using ProjectFlow.Core.DTOs;
using ProjectFlow.Infrastructure.Services;
using TaskState = ProjectFlow.Core.Enums.TaskStatus;

namespace ProjectFlow.IntegrationTests;

public sealed class NotificationTests
{
    [Fact]
    public async Task EventsAndDeadlineSweepsArePrivateIdempotentAndReadStatePersists()
    {
        using var app = new WorkspaceFactory(); await app.InitializeAsync(); using var owner = app.Browser(); using var member = app.Browser();
        await WorkspaceFactory.RegisterAsync(owner, "owner@example.test"); await WorkspaceFactory.RegisterAsync(member, "member@example.test");
        await WorkspaceFactory.LoginAsync(owner, "owner@example.test"); await WorkspaceFactory.LoginAsync(member, "member@example.test");
        var ownerId = (await owner.GetFromJsonAsync<AccountDto>("/api/v1/account/me"))!.Id;
        var memberId = (await member.GetFromJsonAsync<AccountDto>("/api/v1/account/me"))!.Id;
        var org = await IdAsync(await owner.PostAsJsonAsync("/api/v1/organizations", new { name = "Notifications" }));
        Assert.Equal(HttpStatusCode.NoContent, (await owner.PostAsJsonAsync($"/api/v1/organizations/{org}/members", new { email = "member@example.test" })).StatusCode);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var project = await IdAsync(await owner.PostAsJsonAsync("/api/v1/projects", new ProjectRequest { OrganizationId = org, Name = "Event project", ManagerId = ownerId, StartDate = today.AddDays(-10), EndDate = today.AddDays(10) }));
        var p = (await owner.GetFromJsonAsync<ProjectDto>($"/api/v1/projects/{project}"))!;
        Assert.Equal(HttpStatusCode.NoContent, (await owner.PostAsJsonAsync($"/api/v1/projects/{project}/members", new { userId = memberId, version = p.Version })).StatusCode);
        var id = await IdAsync(await owner.PostAsJsonAsync("/api/v1/tasks", new TaskRequest { ProjectId = project, Title = "Overdue event", AssigneeId = memberId, StartDate = today.AddDays(-5), DueDate = today.AddDays(-1) }));
        await IdAsync(await owner.PostAsJsonAsync("/api/v1/tasks", new TaskRequest { ProjectId = project, Title = "Upcoming event", AssigneeId = memberId, StartDate = today, DueDate = today.AddDays(1) }));
        var initial = (await member.GetFromJsonAsync<PageDto<NotificationDto>>("/api/v1/notifications"))!; Assert.Equal(2, initial.Total);
        Assert.Equal(HttpStatusCode.NotFound, (await owner.PutAsync($"/api/v1/notifications/{initial.Items[0].Id}/read", null)).StatusCode);
        var task = (await member.GetFromJsonAsync<TaskDto>($"/api/v1/tasks/{id}"))!;
        Assert.Equal(HttpStatusCode.NoContent, (await member.PutAsJsonAsync($"/api/v1/tasks/{id}/status", new { status = TaskState.InProgress, version = task.Version })).StatusCode);
        Assert.Single((await owner.GetFromJsonAsync<PageDto<NotificationDto>>("/api/v1/notifications"))!.Items);
        Assert.Equal(HttpStatusCode.Created, (await owner.PostAsJsonAsync($"/api/v1/tasks/{id}/comments", new { body = "Please review." })).StatusCode);
        for (var i = 0; i < 2; i++) { using var scope = app.Services.CreateScope(); await scope.ServiceProvider.GetRequiredService<DeadlineService>().SweepAsync(default); }
        var notices = (await member.GetFromJsonAsync<PageDto<NotificationDto>>("/api/v1/notifications"))!;
        Assert.Equal(5, notices.Total); Assert.Single(notices.Items, x => x.Message.StartsWith("Overdue:")); Assert.Single(notices.Items, x => x.Message.StartsWith("Due soon:"));
        Assert.Equal(HttpStatusCode.NoContent, (await member.PutAsync($"/api/v1/notifications/{notices.Items[0].Id}/read", null)).StatusCode);
        Assert.Equal(4, (await member.GetFromJsonAsync<PageDto<NotificationDto>>("/api/v1/notifications?unread=true"))!.Total);
        Assert.Equal(HttpStatusCode.OK, (await member.GetAsync("/Notifications")).StatusCode);
    }
    private static async Task<Guid> IdAsync(HttpResponseMessage response)
    { Assert.Equal(HttpStatusCode.Created, response.StatusCode); return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid(); }
}
