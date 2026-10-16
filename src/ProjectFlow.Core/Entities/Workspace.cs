using ProjectFlow.Core.Enums;
using WorkStatus = ProjectFlow.Core.Enums.TaskStatus;

namespace ProjectFlow.Core.Entities;

public abstract class Entity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid Version { get; set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class Organization : Entity
{
    public string Name { get; set; } = "";
    public string OwnerId { get; set; } = "";
}
public sealed class OrganizationMember
{
    public Guid OrganizationId { get; set; }
    public string UserId { get; set; } = "";
}
public sealed class Project : Entity
{
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string ManagerId { get; set; } = "";
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public ProjectStatus Status { get; set; } = ProjectStatus.Planned;
}
public sealed class ProjectMember
{
    public Guid ProjectId { get; set; }
    public string UserId { get; set; } = "";
}
public sealed class TaskItem : Entity
{
    public Guid ProjectId { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string? AssigneeId { get; set; }
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public WorkStatus Status { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly DueDate { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}
public sealed class TaskComment : Entity
{
    public Guid TaskItemId { get; set; }
    public string AuthorId { get; set; } = "";
    public string Body { get; set; } = "";
}
public sealed class ActivityLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public Guid? TaskItemId { get; set; }
    public string ActorId { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
}
public sealed class Notification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = "";
    public Guid? TaskItemId { get; set; }
    public string Message { get; set; } = "";
    public string EventKey { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ReadAt { get; set; }
}
