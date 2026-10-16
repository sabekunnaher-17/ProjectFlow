using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectFlow.Infrastructure.Services;

namespace ProjectFlow.Web.Controllers;

[Authorize]
public sealed class NotificationsController(NotificationService service) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, bool unread = false, CancellationToken ct = default)
    {
        if (page is < 1 or > 1000000)
            return BadRequest();
        ViewData["Unread"] = unread;
        return View(await service.ListAsync(page, unread, ct));
    }
    [HttpPost]
    public async Task<IActionResult> Read(Guid id, CancellationToken ct)
    {
        await service.ReadAsync(id, ct);
        return RedirectToAction(nameof(Index));
    }
}
