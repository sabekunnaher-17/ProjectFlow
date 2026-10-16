using Microsoft.EntityFrameworkCore;
using ProjectFlow.Core.Entities;
using ProjectFlow.Infrastructure.Data;

namespace ProjectFlow.Infrastructure.Services;

public sealed class NotificationEvents(ApplicationDbContext db, TimeProvider clock)
{
    // Called before the surrounding mutation's SaveChanges; notifications are atomic
    // with their event. Actor never receives a redundant self-notification.
    public void Add(TaskItem task, IEnumerable<string?> recipients, string actorId, string key, string message)
    {
        foreach (var userId in recipients.Where(x => !string.IsNullOrEmpty(x) && x != actorId).Distinct())
            db.Notifications.Add(new()
            {
                UserId = userId!,
                TaskItemId = task.Id,
                EventKey = key,
                Message = message,
                CreatedAt = clock.GetUtcNow()
            });
    }
    public async Task CommentAsync(TaskItem task, Guid commentId, string actorId, CancellationToken ct)
    {
        var members = await db.ProjectMembers.Where(x => x.ProjectId == task.ProjectId).Select(x => x.UserId).ToListAsync(ct);
        Add(task, members, actorId, $"comment:{commentId}", $"New comment on {task.Title}");
    }
}
