using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectFlow.Core.DTOs;
using ProjectFlow.Core.Enums;
using ProjectFlow.Infrastructure.Services;

namespace ProjectFlow.Web.Controllers.Api;

[ApiController, Route("api/v1/admin/users"), Authorize(Roles = Roles.Admin)]
public sealed class AdminApiController(AdminService service) : ControllerBase
{
    [HttpGet] public Task<PageDto<AdminUserDto>> List([StringLength(100)] string? keyword, [Range(1, 1000000)] int page = 1, CancellationToken ct = default) => service.ListAsync(keyword, page, ct);
    [HttpPut("{id}")] public async Task<IActionResult> Update(string id, AdminUserRequest input, CancellationToken ct) { await service.UpdateAsync(id, input, ct); return NoContent(); }
}
