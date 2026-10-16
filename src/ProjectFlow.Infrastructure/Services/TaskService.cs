using Microsoft.EntityFrameworkCore;
using ProjectFlow.Core.Common;
using ProjectFlow.Core.DTOs;
using ProjectFlow.Core.Entities;
using ProjectFlow.Core.Enums;
using ProjectFlow.Core.Interfaces;
using ProjectFlow.Infrastructure.Data;
using TaskState = ProjectFlow.Core.Enums.TaskStatus;

namespace ProjectFlow.Infrastructure.Services;

public sealed class TaskService(ApplicationDbContext db, AccessService access, IActor actor, TimeProvider clock)
{
    public IQueryable<TaskItem> Scoped => db.Tasks.Where(t => access.Projects.Any(p => p.Id == t.ProjectId));
    public IQueryable<TaskItem> Filter(TaskQuery input)
    {
        if (input.From > input.To) throw new DomainException("Due-date range is invalid.");
        var q = Scoped;
        if (input.OpenOnly) q = q.Where(x => x.Status != TaskState.Completed && x.Status != TaskState.Cancelled);
        if (input.ProjectId.HasValue) q = q.Where(x => x.ProjectId == input.ProjectId);
        if (input.Mine) q = q.Where(x => x.AssigneeId == actor.Id);
        if (!string.IsNullOrWhiteSpace(input.Keyword)) q = q.Where(x => x.Title.Contains(input.Keyword) || x.Description.Contains(input.Keyword));
        if (!string.IsNullOrWhiteSpace(input.AssigneeId)) q = q.Where(x => x.AssigneeId == input.AssigneeId);
        if (input.Status.HasValue) q = q.Where(x => x.Status == input.Status);
        if (input.Priority.HasValue) q = q.Where(x => x.Priority == input.Priority);
        if (input.From.HasValue) q = q.Where(x => x.DueDate >= input.From);
        if (input.To.HasValue) q = q.Where(x => x.DueDate <= input.To);
        if (input.Overdue)
        {
            var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
            q = q.Where(x => x.DueDate < today && x.Status != TaskState.Completed && x.Status != TaskState.Cancelled);
        }
        return q;
    }
    public async Task<PageDto<TaskDto>> ListAsync(TaskQuery input, CancellationToken ct)
    {
        var q = Filter(input).AsNoTracking();
        var total = await q.CountAsync(ct);
        var sorted = input.Sort switch { "priority" => q.OrderByDescending(x => x.Priority).ThenBy(x => x.DueDate), "title" => q.OrderBy(x => x.Title).ThenBy(x => x.DueDate), _ => q.OrderBy(x => x.DueDate).ThenByDescending(x => x.Priority) };
        var page = await sorted.ThenBy(x => x.Id).Skip((input.Page - 1) * input.PageSize).Take(input.PageSize).ToListAsync(ct);
        var rows = new List<TaskDto>();
        foreach (var task in page) rows.Add(await MapAsync(task, ct));
        return new(rows, total, input.Page, input.PageSize);
    }
    public async Task<TaskDto> GetAsync(Guid id, CancellationToken ct) => await MapAsync(await FindAsync(id, ct), ct);
    public async Task<Guid> CreateAsync(TaskRequest input, CancellationToken ct)
    {
        var p = await access.ProjectAsync(input.ProjectId, true, ct); Writable(p);
        await ValidateAsync(input, p, ct);
        var task = new TaskItem { ProjectId = p.Id }; Apply(task, input); db.Tasks.Add(task);
        Touch(p, task, "Task created."); await db.SaveChangesAsync(ct); return task.Id;
    }
    public async Task UpdateAsync(Guid id, TaskRequest input, CancellationToken ct)
    {
        var task = await FindAsync(id, ct); var p = await access.ProjectAsync(task.ProjectId, true, ct); Writable(p);
        WorkflowRules.Version(input.Version, task.Version);
        if (task.Status is TaskState.Completed or TaskState.Cancelled) throw new DomainException("Reopen the task before editing it.", FailureKind.Conflict);
        if (input.ProjectId != task.ProjectId) throw new DomainException("Tasks cannot be moved between projects.");
        await ValidateAsync(input, p, ct); Apply(task, input);
        Touch(p, task, "Task updated."); await db.SaveChangesAsync(ct);
    }
    public async Task StatusAsync(Guid id, StatusRequest input, CancellationToken ct)
    {
        var task = await FindAsync(id, ct); var p = await access.ProjectAsync(task.ProjectId, false, ct); Writable(p);
        var manages = await access.CanManageAsync(p, ct);
        if (!manages && task.AssigneeId != actor.Id) throw new DomainException("Only the assignee or project management may update this task.", FailureKind.Forbidden);
        WorkflowRules.Version(input.Version, task.Version); WorkflowRules.Transition(task.Status, input.Status, manages);
        if (task.Status == input.Status) return;
        task.Status = input.Status; task.CompletedAt = task.Status == TaskState.Completed ? clock.GetUtcNow() : null;
        Touch(p, task, $"Task status changed to {input.Status}."); await db.SaveChangesAsync(ct);
    }
    private async Task<TaskItem> FindAsync(Guid id, CancellationToken ct) => await Scoped.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new DomainException("Task not found.", FailureKind.NotFound);
    private async Task ValidateAsync(TaskRequest input, Project p, CancellationToken ct)
    {
        WorkflowRules.TaskDates(input.StartDate, input.DueDate, p.StartDate, p.EndDate);
        if (!Enum.IsDefined(input.Priority)) throw new DomainException("Choose a valid priority.");
        if (!string.IsNullOrEmpty(input.AssigneeId) && (!await db.ProjectMembers.AnyAsync(x => x.ProjectId == p.Id && x.UserId == input.AssigneeId, ct)
            || !await db.Users.AnyAsync(x => x.Id == input.AssigneeId && x.IsActive, ct))) throw new DomainException("Assignee must be an active project member.");
    }
    private static void Apply(TaskItem t, TaskRequest i)
    {
        t.Title = i.Title.Trim(); t.Description = i.Description.Trim(); t.AssigneeId = string.IsNullOrWhiteSpace(i.AssigneeId) ? null : i.AssigneeId;
        t.Priority = i.Priority; t.StartDate = i.StartDate; t.DueDate = i.DueDate;
    }
    private static void Writable(Project p)
    {
        WorkflowRules.Editable(p.Status);
        if (p.Status == ProjectStatus.Completed) throw new DomainException("Reopen the project before changing its tasks.", FailureKind.Conflict);
    }
    private void Touch(Project p, TaskItem task, string message)
    {
        // Serializes task changes against project dates/archive/membership changes.
        db.Entry(p).Property(x => x.UpdatedAt).IsModified = true;
        db.Activities.Add(new() { ProjectId = p.Id, TaskItemId = task.Id, ActorId = actor.Id, Description = message, CreatedAt = clock.GetUtcNow() });
    }
    private async Task<TaskDto> MapAsync(TaskItem t, CancellationToken ct)
    {
        var p = await access.ProjectAsync(t.ProjectId, false, ct);
        var manages = await access.CanManageAsync(p, ct);
        var writable = p.Status is not ProjectStatus.Archived and not ProjectStatus.Completed;
        var canChange = writable && (manages || t.AssigneeId == actor.Id);
        var assignee = t.AssigneeId is null ? "Unassigned" : await db.Users.Where(x => x.Id == t.AssigneeId).Select(x => x.DisplayName).SingleAsync(ct);
        return new(t.Id, p.Id, p.Name, t.Title, t.Description, t.AssigneeId, assignee, t.Priority, t.Status, t.StartDate, t.DueDate, t.CompletedAt, t.Version,
            t.DueDate < DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime) && t.Status is not TaskState.Completed and not TaskState.Cancelled,
            manages && writable, canChange, canChange ? Enum.GetValues<TaskState>().Where(s => s != t.Status && WorkflowRules.CanTransition(t.Status, s, manages)).ToArray() : [], p.Status != ProjectStatus.Archived);
    }
}
