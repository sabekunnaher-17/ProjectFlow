using Microsoft.EntityFrameworkCore;
using ProjectFlow.Core.Common;
using ProjectFlow.Core.DTOs;
using ProjectFlow.Core.Interfaces;
using ProjectFlow.Infrastructure.Data;

namespace ProjectFlow.Infrastructure.Services;

public sealed class NotificationService(ApplicationDbContext db, TaskService tasks, IActor actor, TimeProvider clock)
{
    private IQueryable<Core.Entities.Notification> Visible => db.Notifications.Where(n => n.UserId == actor.Id && (n.TaskItemId == null || tasks.Scoped.Any(t => t.Id == n.TaskItemId)));
    public async Task<PageDto<NotificationDto>> ListAsync(int page, bool unread, CancellationToken ct)
    {
        var q = Visible;
        if (unread)
            q = q.Where(x => x.ReadAt == null);
        return new(await q.OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id).Skip((page - 1) * 20).Take(20)
            .Select(x => new NotificationDto(x.Id, x.TaskItemId, x.Message, x.CreatedAt, x.ReadAt)).ToListAsync(ct), await q.CountAsync(ct), page, 20);
    }
    public async Task ReadAsync(Guid id, CancellationToken ct)
    {
        var item = await Visible.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new DomainException("Notification not found.", FailureKind.NotFound);
        item.ReadAt ??= clock.GetUtcNow();
        await db.SaveChangesAsync(ct);
    }
}
