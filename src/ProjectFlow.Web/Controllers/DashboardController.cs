using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectFlow.Infrastructure.Services;

namespace ProjectFlow.Web.Controllers;

[Authorize]
public sealed class DashboardController(InsightService service) : Controller
{
    [HttpGet] public async Task<IActionResult> Index(CancellationToken ct) => View(await service.DashboardAsync(ct));
    [HttpGet]
    public async Task<IActionResult> Activity(Guid? projectId, int page = 1, CancellationToken ct = default)
    {
        if (page is < 1 or > 1000000)
            return BadRequest();
        ViewData["ProjectId"] = projectId;
        return View(await service.ActivitiesAsync(projectId, page, ct));
    }
}
