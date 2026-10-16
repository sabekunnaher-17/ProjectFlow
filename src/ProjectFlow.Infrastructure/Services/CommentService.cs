using Microsoft.EntityFrameworkCore;
using ProjectFlow.Core.Common;
using ProjectFlow.Core.DTOs;
using ProjectFlow.Core.Entities;
using ProjectFlow.Core.Interfaces;
using ProjectFlow.Infrastructure.Data;

namespace ProjectFlow.Infrastructure.Services;

public sealed class CommentService(ApplicationDbContext db, TaskService tasks, AccessService access, IActor actor, TimeProvider clock)
{
    public async Task<PageDto<CommentDto>> ListAsync(Guid taskId, int page, CancellationToken ct)
    {
        await tasks.GetAsync(taskId, ct);
        var q = from c in db.Comments join u in db.Users on c.AuthorId equals u.Id where c.TaskItemId == taskId select new { Comment = c, User = u };
        return new(await q.OrderByDescending(x => x.Comment.CreatedAt).ThenBy(x => x.Comment.Id).Skip((page - 1) * 20).Take(20)
            .Select(x => new CommentDto(x.Comment.Id, x.User.DisplayName, x.Comment.Body, x.Comment.CreatedAt)).ToListAsync(ct), await q.CountAsync(ct), page, 20);
    }
    public async Task AddAsync(Guid taskId, CommentRequest input, CancellationToken ct)
    {
        var task = await tasks.GetAsync(taskId, ct); var p = await access.ProjectAsync(task.ProjectId, false, ct); WorkflowRules.Editable(p.Status);
        db.Comments.Add(new TaskComment { TaskItemId = taskId, AuthorId = actor.Id, Body = input.Body.Trim() });
        db.Entry(p).Property(x => x.UpdatedAt).IsModified = true;
        db.Activities.Add(new() { ProjectId = p.Id, TaskItemId = taskId, ActorId = actor.Id, Description = "Task comment added.", CreatedAt = clock.GetUtcNow() });
        await db.SaveChangesAsync(ct);
    }
}
