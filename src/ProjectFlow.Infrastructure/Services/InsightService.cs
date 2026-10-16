using System.Text;
using Microsoft.EntityFrameworkCore;
using ProjectFlow.Core.Common;
using ProjectFlow.Core.DTOs;
using ProjectFlow.Core.Enums;
using ProjectFlow.Infrastructure.Data;
using TaskState = ProjectFlow.Core.Enums.TaskStatus;

namespace ProjectFlow.Infrastructure.Services;

public sealed class InsightService(ApplicationDbContext db, AccessService access, TaskService tasks, TimeProvider clock)
{
    public async Task<PageDto<ActivityDto>> ActivitiesAsync(Guid? projectId, int page, CancellationToken ct)
    {
        if (projectId.HasValue)
            await access.ProjectAsync(projectId.Value, false, ct);
        var q = from a in db.Activities
                join p in access.Projects on a.ProjectId equals p.Id
                join u in db.Users on a.ActorId equals u.Id
                where !projectId.HasValue || a.ProjectId == projectId
                select new
                {
                    Activity = a,
                    Project = p,
                    User = u
                };
        return new(await q.OrderByDescending(x => x.Activity.CreatedAt).ThenBy(x => x.Activity.Id).Skip((page - 1) * 20).Take(20)
            .Select(x => new ActivityDto(x.Activity.Id, x.Project.Id, x.Activity.TaskItemId, x.Project.Name, x.User.DisplayName, x.Activity.Description, x.Activity.CreatedAt)).ToListAsync(ct), await q.CountAsync(ct), page, 20);
    }
    public async Task<DashboardDto> DashboardAsync(CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        var counts = await tasks.Scoped.GroupBy(x => x.Status).Select(g => new StatusCount(g.Key, g.Count())).ToListAsync(ct);
        int Count(TaskState status) => counts.SingleOrDefault(x => x.Status == status)?.Count ?? 0;
        var upcoming = await tasks.ListAsync(new TaskQuery { From = today, To = today.AddDays(7), PageSize = 5, OpenOnly = true }, ct);
        return new(await access.Projects.CountAsync(ct), await access.Projects.CountAsync(x => x.Status == ProjectStatus.Active, ct),
            counts.Sum(x => x.Count), Count(TaskState.Todo), Count(TaskState.InProgress) + Count(TaskState.InReview), Count(TaskState.Completed),
            await tasks.Filter(new()
            {
                Overdue = true
            }).CountAsync(ct), counts, upcoming.Items.Where(x => x.Status is not TaskState.Completed and not TaskState.Cancelled).ToArray(), (await ActivitiesAsync(null, 1, ct)).Items);
    }
    public async Task<string> ExportAsync(TaskQuery input, CancellationToken ct)
    {
        // Bounded export: never silently truncate a report.
        var q = tasks.Filter(input);
        if (await q.CountAsync(ct) > 5000)
            throw new DomainException("Narrow report filters to at most 5,000 tasks.");
        var rows = await (from t in q
                          join p in db.Projects on t.ProjectId equals p.Id
                          join u in db.Users on t.AssigneeId equals u.Id into assignees
                          from u in assignees.DefaultIfEmpty()
                          orderby t.DueDate, t.Id
                          select new
                          {
                              t.Title,
                              Project = p.Name,
                              Assignee = u == null ? "Unassigned" : u.DisplayName,
                              t.Status,
                              t.Priority,
                              t.StartDate,
                              t.DueDate
                          }).ToListAsync(ct);
        var csv = new StringBuilder("Title,Project,Assignee,Status,Priority,Start date,Due date\r\n");
        foreach (var r in rows)
            csv.Append(string.Join(",", new[] { r.Title, r.Project, r.Assignee, r.Status.ToString(), r.Priority.ToString(), r.StartDate.ToString("yyyy-MM-dd"), r.DueDate.ToString("yyyy-MM-dd") }.Select(Csv.Cell))).Append("\r\n");
        return csv.ToString();
    }
}
