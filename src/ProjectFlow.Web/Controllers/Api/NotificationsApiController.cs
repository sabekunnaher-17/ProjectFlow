using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectFlow.Core.DTOs;
using ProjectFlow.Infrastructure.Services;

namespace ProjectFlow.Web.Controllers.Api;

[ApiController, Route("api/v1/notifications"), Authorize]
public sealed class NotificationsApiController(NotificationService service) : ControllerBase
{
    [HttpGet] public Task<PageDto<NotificationDto>> List([Range(1, 1000000)] int page = 1, bool unread = false, CancellationToken ct = default) => service.ListAsync(page, unread, ct);
    [HttpPut("{id:guid}/read")] public async Task<IActionResult> Read(Guid id, CancellationToken ct) { await service.ReadAsync(id, ct); return NoContent(); }
}
