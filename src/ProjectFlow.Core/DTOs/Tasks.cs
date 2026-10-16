using System.ComponentModel.DataAnnotations;
using ProjectFlow.Core.Enums;
using TaskState = ProjectFlow.Core.Enums.TaskStatus;

namespace ProjectFlow.Core.DTOs;

public sealed class TaskRequest
{
    public Guid ProjectId { get; set; }
    [Required, StringLength(160, MinimumLength = 2)] public string Title { get; set; } = "";
    [StringLength(5000)] public string Description { get; set; } = "";
    [StringLength(128)] public string? AssigneeId { get; set; }
    [EnumDataType(typeof(TaskPriority))] public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    [DataType(DataType.Date)] public DateOnly StartDate { get; set; }
    [DataType(DataType.Date)] public DateOnly DueDate { get; set; }
    public Guid Version { get; set; }
}
public sealed class StatusRequest
{
    [EnumDataType(typeof(TaskState))] public TaskState Status { get; set; }
    public Guid Version { get; set; }
}
public sealed class TaskQuery
{
    [StringLength(160)] public string? Keyword { get; set; }
    public Guid? ProjectId { get; set; }
    [StringLength(128)] public string? AssigneeId { get; set; }
    [EnumDataType(typeof(TaskState))] public TaskState? Status { get; set; }
    [EnumDataType(typeof(TaskPriority))] public TaskPriority? Priority { get; set; }
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
    public bool Overdue { get; set; }
    public bool Mine { get; set; }
    public bool OpenOnly { get; set; }
    [RegularExpression("^(due|priority|title)$")] public string Sort { get; set; } = "due";
    [Range(1, 1000000)] public int Page { get; set; } = 1;
    [Range(1, 100)] public int PageSize { get; set; } = 20;
}
public sealed record TaskDto(Guid Id, Guid ProjectId, string ProjectName, string Title, string Description,
    string? AssigneeId, string AssigneeName, TaskPriority Priority, TaskState Status, DateOnly StartDate,
    DateOnly DueDate, DateTimeOffset? CompletedAt, Guid Version, bool Overdue, bool CanManage, bool CanChangeStatus,
    IReadOnlyList<TaskState> AllowedStatuses, bool CanComment);
