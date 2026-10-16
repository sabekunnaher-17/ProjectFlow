using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectFlow.Core.DTOs;
using ProjectFlow.Infrastructure.Services;

namespace ProjectFlow.Web.Controllers.Api;

[ApiController, Route("api/v1/projects"), Authorize]
public sealed class ProjectsApiController(ProjectService service) : ControllerBase
{
    [HttpGet] public Task<PageDto<ProjectDto>> List([FromQuery] ProjectQuery input, CancellationToken ct) => service.ListAsync(input, ct);
    [HttpGet("{id:guid}")] public Task<ProjectDto> Get(Guid id, CancellationToken ct) => service.GetAsync(id, ct);
    [HttpPost]
    public async Task<IActionResult> Create(ProjectRequest input, CancellationToken ct)
    {
        var id = await service.CreateAsync(input, ct);
        return CreatedAtAction(nameof(Get), new
        {
            id
        }, new
        {
            id
        });
    }
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Edit(Guid id, ProjectRequest input, CancellationToken ct)
    {
        await service.UpdateAsync(id, input, ct);
        return NoContent();
    }
    [HttpPost("{id:guid}/archive")]
    public async Task<IActionResult> Archive(Guid id, VersionRequest input, CancellationToken ct)
    {
        await service.ArchiveAsync(id, input.Version, ct);
        return NoContent();
    }
    [HttpPost("{id:guid}/members")]
    public async Task<IActionResult> Add(Guid id, MemberRequest input, CancellationToken ct)
    {
        await service.MembershipAsync(id, input, false, ct);
        return NoContent();
    }
    [HttpDelete("{id:guid}/members/{userId}")]
    public async Task<IActionResult> Remove(Guid id, string userId, VersionRequest input, CancellationToken ct)
    {
        await service.MembershipAsync(id, new()
        {
            UserId = userId,
            Version = input.Version
        }, true, ct);
        return NoContent();
    }
}
