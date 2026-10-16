using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProjectFlow.Core.Entities;
using ProjectFlow.Infrastructure.Data;

namespace ProjectFlow.IntegrationTests;

public sealed class SqlConstraintTests
{
    [SqlServerFact]
    public async Task SqlServerRejectsInvalidAssignmentDatesAndDuplicateEvents()
    {
        using var app = new WorkspaceFactory();
        await app.InitializeAsync();
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var owner = new ApplicationUser { DisplayName = "Constraint owner" };
        var other = new ApplicationUser { DisplayName = "Not a project member" };
        db.Users.AddRange(owner, other);
        var org = new Organization { Name = "Constraint workspace", OwnerId = owner.Id };
        db.Organizations.Add(org);
        var project = new Project { OrganizationId = org.Id, ManagerId = owner.Id, Name = "Constraint project", StartDate = new(2026, 1, 1), EndDate = new(2026, 12, 31) };
        db.Projects.Add(project);
        db.ProjectMembers.Add(new()
        {
            ProjectId = project.Id,
            UserId = owner.Id
        });
        await db.SaveChangesAsync();
        db.Tasks.Add(new()
        {
            ProjectId = project.Id,
            AssigneeId = other.Id,
            Title = "Invalid assignment",
            StartDate = new(2026, 1, 1),
            DueDate = new(2026, 2, 1)
        });
        var invalidAssignment = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        Assert.Contains("FK_Tasks_ProjectMembers", invalidAssignment.InnerException!.Message);
        db.ChangeTracker.Clear();
        db.Tasks.Add(new()
        {
            ProjectId = project.Id,
            AssigneeId = owner.Id,
            Title = "Invalid dates",
            StartDate = new(2026, 2, 1),
            DueDate = new(2026, 1, 1)
        });
        var invalidDates = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        Assert.Contains("CK_Task_Dates", invalidDates.InnerException!.Message);
        db.ChangeTracker.Clear();
        db.Notifications.AddRange(new Notification { UserId = owner.Id, Message = "Duplicate", EventKey = "same-event" }, new Notification { UserId = owner.Id, Message = "Duplicate", EventKey = "same-event" });
        var duplicate = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        Assert.Contains("IX_Notifications_UserId_EventKey", duplicate.InnerException!.Message);
        db.ChangeTracker.Clear();
        Assert.Equal(0, await db.Tasks.CountAsync());
        Assert.Equal(0, await db.Notifications.CountAsync());
    }
}
