using System.ComponentModel.DataAnnotations;
using TaskState = ProjectFlow.Core.Enums.TaskStatus;

namespace ProjectFlow.Core.DTOs;

public sealed class CommentRequest
{
    [Required, StringLength(3000, MinimumLength = 1)] public string Body { get; set; } = "";
}
public sealed record CommentDto(Guid Id, string AuthorName, string Body, DateTimeOffset CreatedAt);
public sealed record ActivityDto(Guid Id, Guid ProjectId, Guid? TaskItemId, string ProjectName, string ActorName, string Description, DateTimeOffset CreatedAt);
public sealed record StatusCount(TaskState Status, int Count);
public sealed record DashboardDto(int TotalProjects, int ActiveProjects, int TotalTasks, int PendingTasks, int InProgressTasks,
    int CompletedTasks, int OverdueTasks, IReadOnlyList<StatusCount> Statuses, IReadOnlyList<TaskDto> Upcoming, IReadOnlyList<ActivityDto> Recent);
