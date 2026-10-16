using Microsoft.EntityFrameworkCore;
using ProjectFlow.Core.Entities;
using ProjectFlow.Core.Enums;
using ProjectFlow.Infrastructure.Data;
using TaskState = ProjectFlow.Core.Enums.TaskStatus;

namespace ProjectFlow.Infrastructure.Services;

public sealed class DeadlineService(ApplicationDbContext db, TimeProvider clock)
{
    public async Task SweepAsync(CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await DatabaseGate.LockAsync(db, false, ct);
        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        // Batch by IDs; exclude closed/archived work and inactive recipients.
        var query = from t in db.Tasks join p in db.Projects on t.ProjectId equals p.Id join u in db.Users on t.AssigneeId equals u.Id
            where u.IsActive && p.Status != ProjectStatus.Archived && p.Status != ProjectStatus.Completed
                && t.Status != TaskState.Completed && t.Status != TaskState.Cancelled && t.DueDate <= today.AddDays(2)
            orderby t.Id select t;
        var count = await query.CountAsync(ct);
        for (var offset = 0; offset < count; offset += 200)
        {
            var batch = await query.Skip(offset).Take(200).ToListAsync(ct);
            foreach (var t in batch)
            {
                var overdue = t.DueDate < today;
                // Once per due date and category, not once per page request/poll.
                var key = $"deadline:{t.Id}:{t.DueDate:yyyyMMdd}:{(overdue ? "overdue" : "upcoming")}";
                if (await db.Notifications.AnyAsync(n => n.UserId == t.AssigneeId && n.EventKey == key, ct)) continue;
                db.Notifications.Add(new Notification { UserId = t.AssigneeId!, TaskItemId = t.Id, EventKey = key,
                    Message = $"{(overdue ? "Overdue" : "Due soon")}: {t.Title}", CreatedAt = clock.GetUtcNow() });
            }
            await db.SaveChangesAsync(ct);
        }
        await transaction.CommitAsync(ct);
    }
}
