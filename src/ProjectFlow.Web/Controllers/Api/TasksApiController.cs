using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectFlow.Core.DTOs;
using ProjectFlow.Infrastructure.Services;

namespace ProjectFlow.Web.Controllers.Api;

[ApiController, Route("api/v1/tasks"), Authorize]
public sealed class TasksApiController(TaskService service) : ControllerBase
{
    [HttpGet] public Task<PageDto<TaskDto>> List([FromQuery] TaskQuery input, CancellationToken ct) => service.ListAsync(input, ct);
    [HttpGet("{id:guid}")] public Task<TaskDto> Get(Guid id, CancellationToken ct) => service.GetAsync(id, ct);
    [HttpPost]
    public async Task<IActionResult> Create(TaskRequest input, CancellationToken ct)
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
    public async Task<IActionResult> Edit(Guid id, TaskRequest input, CancellationToken ct)
    {
        await service.UpdateAsync(id, input, ct);
        return NoContent();
    }
    [HttpPut("{id:guid}/status")]
    public async Task<IActionResult> Status(Guid id, StatusRequest input, CancellationToken ct)
    {
        await service.StatusAsync(id, input, ct);
        return NoContent();
    }
}
