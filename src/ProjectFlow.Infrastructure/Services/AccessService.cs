using Microsoft.EntityFrameworkCore;
using ProjectFlow.Core.Common;
using ProjectFlow.Core.Entities;
using ProjectFlow.Core.Interfaces;
using ProjectFlow.Infrastructure.Data;

namespace ProjectFlow.Infrastructure.Services;

public sealed class AccessService(ApplicationDbContext db, IActor actor)
{
    public IQueryable<Project> Projects => db.Projects.Where(p => actor.IsAdmin || p.ManagerId == actor.Id
        || db.Organizations.Any(o => o.Id == p.OrganizationId && o.OwnerId == actor.Id)
        || db.ProjectMembers.Any(m => m.ProjectId == p.Id && m.UserId == actor.Id));
    public IQueryable<Organization> Organizations => db.Organizations.Where(o => actor.IsAdmin || o.OwnerId == actor.Id
        || db.OrganizationMembers.Any(m => m.OrganizationId == o.Id && m.UserId == actor.Id));
    public async Task<Organization> OrganizationAsync(Guid id, bool manage, CancellationToken ct)
    {
        var org = await Organizations.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new DomainException("Organization not found.", FailureKind.NotFound);
        if (manage && !actor.IsAdmin && org.OwnerId != actor.Id)
            throw new DomainException("Organization ownership is required.", FailureKind.Forbidden);
        return org;
    }
    public async Task<Project> ProjectAsync(Guid id, bool manage, CancellationToken ct)
    {
        var project = await Projects.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new DomainException("Project not found.", FailureKind.NotFound);
        if (manage && !await CanManageAsync(project, ct))
            throw new DomainException("Project management permission is required.", FailureKind.Forbidden);
        return project;
    }
    public async Task<bool> CanManageAsync(Project project, CancellationToken ct) => actor.IsAdmin || project.ManagerId == actor.Id
        || await db.Organizations.AnyAsync(x => x.Id == project.OrganizationId && x.OwnerId == actor.Id, ct);
}
