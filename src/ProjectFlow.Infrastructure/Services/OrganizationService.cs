using Microsoft.EntityFrameworkCore;
using ProjectFlow.Core.Common;
using ProjectFlow.Core.DTOs;
using ProjectFlow.Core.Entities;
using ProjectFlow.Core.Interfaces;
using ProjectFlow.Infrastructure.Data;

namespace ProjectFlow.Infrastructure.Services;

public sealed class OrganizationService(ApplicationDbContext db, AccessService access, IActor actor)
{
    public async Task<IReadOnlyList<OrganizationDto>> ListAsync(CancellationToken ct)
    {
        var orgs = await access.Organizations.OrderBy(x => x.Name).Take(100).ToListAsync(ct);
        return orgs.Select(x => new OrganizationDto(x.Id, x.Name, x.OwnerId, actor.IsAdmin || x.OwnerId == actor.Id, [])).ToArray();
    }
    public async Task<OrganizationDto> GetAsync(Guid id, CancellationToken ct)
    {
        var org = await access.OrganizationAsync(id, false, ct);
        var members = await (from m in db.OrganizationMembers
                             join u in db.Users on m.UserId equals u.Id
                             where m.OrganizationId == id
                             orderby u.DisplayName
                             select new MemberDto(u.Id, u.DisplayName, u.Email!)).ToListAsync(ct);
        return new(org.Id, org.Name, org.OwnerId, actor.IsAdmin || org.OwnerId == actor.Id, members);
    }
    public async Task<Guid> CreateAsync(OrganizationRequest input, CancellationToken ct)
    {
        var org = new Organization { Name = input.Name.Trim(), OwnerId = actor.Id };
        db.Organizations.Add(org);
        db.OrganizationMembers.Add(new()
        {
            OrganizationId = org.Id,
            UserId = actor.Id
        });
        await db.SaveChangesAsync(ct);
        return org.Id;
    }
    public async Task AddMemberAsync(Guid id, OrganizationMemberRequest input, CancellationToken ct)
    {
        await access.OrganizationAsync(id, true, ct);
        var normalized = input.Email.Trim().ToUpperInvariant();
        var user = await db.Users.SingleOrDefaultAsync(x => x.NormalizedEmail == normalized && x.IsActive, ct)
            ?? throw new DomainException("An active registered account is required.");
        if (await db.OrganizationMembers.AnyAsync(x => x.OrganizationId == id && x.UserId == user.Id, ct))
            return;
        db.OrganizationMembers.Add(new()
        {
            OrganizationId = id,
            UserId = user.Id
        });
        await db.SaveChangesAsync(ct);
    }
}
