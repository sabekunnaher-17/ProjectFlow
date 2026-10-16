using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectFlow.Core.DTOs;
using ProjectFlow.Infrastructure.Services;

namespace ProjectFlow.Web.Controllers.Api;

[ApiController, Route("api/v1/organizations"), Authorize]
public sealed class OrganizationsApiController(OrganizationService service) : ControllerBase
{
    [HttpGet] public Task<IReadOnlyList<OrganizationDto>> List(CancellationToken ct) => service.ListAsync(ct);
    [HttpGet("{id:guid}")] public Task<OrganizationDto> Get(Guid id, CancellationToken ct) => service.GetAsync(id, ct);
    [HttpPost]
    public async Task<IActionResult> Create(OrganizationRequest input, CancellationToken ct)
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
    [HttpPost("{id:guid}/members")]
    public async Task<IActionResult> Add(Guid id, OrganizationMemberRequest input, CancellationToken ct)
    {
        await service.AddMemberAsync(id, input, ct);
        return NoContent();
    }
}
