using Microsoft.EntityFrameworkCore;
using ProjectFlow.Core.Common;
using ProjectFlow.Core.DTOs;
using ProjectFlow.Core.Entities;
using ProjectFlow.Core.Enums;
using ProjectFlow.Core.Interfaces;
using ProjectFlow.Infrastructure.Data;

namespace ProjectFlow.Infrastructure.Services;

public sealed class ProjectService(ApplicationDbContext db, AccessService access, IActor actor, TimeProvider clock)
{
    public async Task<PageDto<ProjectDto>> ListAsync(ProjectQuery input, CancellationToken ct)
    {
        var q = access.Projects.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(input.Keyword))
            q = q.Where(x => x.Name.Contains(input.Keyword) || x.Description.Contains(input.Keyword));
        if (input.Status.HasValue)
            q = q.Where(x => x.Status == input.Status);
        if (input.OrganizationId.HasValue)
            q = q.Where(x => x.OrganizationId == input.OrganizationId);
        var total = await q.CountAsync(ct);
        var page = await q.OrderBy(x => x.Name).ThenBy(x => x.Id).Skip((input.Page - 1) * input.PageSize).Take(input.PageSize).ToListAsync(ct);
        var rows = new List<ProjectDto>();
        foreach (var item in page)
            rows.Add(await MapAsync(item, false, ct));
        return new(rows, total, input.Page, input.PageSize);
    }
    public async Task<ProjectDto> GetAsync(Guid id, CancellationToken ct) => await MapAsync(await access.ProjectAsync(id, false, ct), true, ct);
    public async Task<Guid> CreateAsync(ProjectRequest input, CancellationToken ct)
    {
        var org = await access.OrganizationAsync(input.OrganizationId, false, ct);
        if (!actor.IsAdmin && !actor.IsManager && org.OwnerId != actor.Id)
            throw new DomainException("Organization ownership or a scoped manager role is required.", FailureKind.Forbidden);
        await ValidateAsync(input, org, ct);
        var p = new Project { OrganizationId = org.Id };
        Apply(p, input);
        db.Projects.Add(p);
        db.ProjectMembers.Add(new()
        {
            ProjectId = p.Id,
            UserId = p.ManagerId
        });
        if (actor.Id != p.ManagerId && await db.OrganizationMembers.AnyAsync(x => x.OrganizationId == org.Id && x.UserId == actor.Id, ct))
            db.ProjectMembers.Add(new()
            {
                ProjectId = p.Id,
                UserId = actor.Id
            });
        Activity(p.Id, "Project created.");
        await db.SaveChangesAsync(ct);
        return p.Id;
    }
    public async Task UpdateAsync(Guid id, ProjectRequest input, CancellationToken ct)
    {
        var p = await access.ProjectAsync(id, true, ct);
        WorkflowRules.Editable(p.Status);
        WorkflowRules.Version(input.Version, p.Version);
        if (p.OrganizationId != input.OrganizationId)
            throw new DomainException("A project's organization cannot be changed.");
        var org = await access.OrganizationAsync(p.OrganizationId, false, ct);
        await ValidateAsync(input, org, ct);
        if (await db.Tasks.AnyAsync(x => x.ProjectId == id && (x.StartDate < input.StartDate || x.DueDate > input.EndDate), ct))
            throw new DomainException("Project dates must include all existing task dates.");
        if (input.Status == ProjectStatus.Completed && await db.Tasks.AnyAsync(x => x.ProjectId == id && x.Status != Core.Enums.TaskStatus.Completed && x.Status != Core.Enums.TaskStatus.Cancelled, ct))
            throw new DomainException("Complete or cancel all tasks before completing the project.");
        Apply(p, input);
        if (!await db.ProjectMembers.AnyAsync(x => x.ProjectId == id && x.UserId == p.ManagerId, ct))
            db.ProjectMembers.Add(new()
            {
                ProjectId = id,
                UserId = p.ManagerId
            });
        Activity(id, "Project updated.");
        await db.SaveChangesAsync(ct);
    }
    public async Task ArchiveAsync(Guid id, Guid version, CancellationToken ct)
    {
        var p = await access.ProjectAsync(id, true, ct);
        WorkflowRules.Version(version, p.Version);
        p.Status = ProjectStatus.Archived;
        Activity(id, "Project archived.");
        await db.SaveChangesAsync(ct);
    }
    public async Task MembershipAsync(Guid id, MemberRequest input, bool remove, CancellationToken ct)
    {
        var p = await access.ProjectAsync(id, true, ct);
        WorkflowRules.Editable(p.Status);
        WorkflowRules.Version(input.Version, p.Version);
        var membership = await db.ProjectMembers.SingleOrDefaultAsync(x => x.ProjectId == id && x.UserId == input.UserId, ct);
        if (remove)
        {
            if (input.UserId == p.ManagerId)
                throw new DomainException("Assign another manager before removing this member.");
            if (await db.Tasks.AnyAsync(x => x.ProjectId == id && x.AssigneeId == input.UserId, ct))
                throw new DomainException("Reassign this member's tasks before removing them.");
            if (membership is not null)
                db.ProjectMembers.Remove(membership);
        }
        else
        {
            if (!await db.OrganizationMembers.AnyAsync(x => x.OrganizationId == p.OrganizationId && x.UserId == input.UserId, ct)
                || !await db.Users.AnyAsync(x => x.Id == input.UserId && x.IsActive, ct))
                throw new DomainException("Choose an active organization member.");
            if (membership is null)
                db.ProjectMembers.Add(new()
                {
                    ProjectId = id,
                    UserId = input.UserId
                });
        }
        db.Entry(p).Property(x => x.UpdatedAt).IsModified = true;
        Activity(id, remove ? "Project member removed." : "Project member added.");
        await db.SaveChangesAsync(ct);
    }
    private async Task ValidateAsync(ProjectRequest input, Organization org, CancellationToken ct)
    {
        WorkflowRules.Dates(input.StartDate, input.EndDate);
        if (!Enum.IsDefined(input.Status))
            throw new DomainException("Choose a valid project status.");
        if (!await db.OrganizationMembers.AnyAsync(x => x.OrganizationId == org.Id && x.UserId == input.ManagerId, ct)
            || !await db.Users.AnyAsync(x => x.Id == input.ManagerId && x.IsActive, ct))
            throw new DomainException("Manager must be an active organization member.");
        // Management is an explicit project assignment, not access to all organizations.
    }
    private static void Apply(Project p, ProjectRequest i)
    {
        p.Name = i.Name.Trim();
        p.Description = i.Description?.Trim() ?? "";
        p.ManagerId = i.ManagerId;
        p.StartDate = i.StartDate;
        p.EndDate = i.EndDate;
        p.Status = i.Status;
    }
    private void Activity(Guid id, string message) => db.Activities.Add(new() { ProjectId = id, ActorId = actor.Id, Description = message, CreatedAt = clock.GetUtcNow() });
    private async Task<ProjectDto> MapAsync(Project p, bool details, CancellationToken ct)
    {
        var orgName = await db.Organizations.Where(x => x.Id == p.OrganizationId).Select(x => x.Name).SingleAsync(ct);
        var managerName = await db.Users.Where(x => x.Id == p.ManagerId).Select(x => x.DisplayName).SingleAsync(ct);
        var statuses = await db.Tasks.Where(x => x.ProjectId == p.Id).Select(x => x.Status).ToListAsync(ct);
        var members = details ? await (from m in db.ProjectMembers join u in db.Users on m.UserId equals u.Id where m.ProjectId == p.Id orderby u.DisplayName select new MemberDto(u.Id, u.DisplayName, u.Email!)).ToListAsync(ct) : [];
        return new(p.Id, p.OrganizationId, orgName, p.Name, p.Description, p.ManagerId, managerName, p.StartDate, p.EndDate, p.Status, p.Version, WorkflowRules.Progress(statuses), await access.CanManageAsync(p, ct), members);
    }
}
