using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectFlow.Core.DTOs;
using ProjectFlow.Core.Enums;
using ProjectFlow.Infrastructure.Services;

namespace ProjectFlow.Web.Controllers;

[Authorize(Roles = Roles.Admin)]
public sealed class AdminController(AdminService service) : Controller
{
    [HttpGet] public async Task<IActionResult> Index(string? keyword, int page = 1, CancellationToken ct = default)
    { if (page is < 1 or > 1000000 || keyword?.Length > 100) return BadRequest(); ViewData["Keyword"] = keyword; return View(await service.ListAsync(keyword, page, ct)); }
    [HttpPost] public async Task<IActionResult> Update(string id, AdminUserRequest input, CancellationToken ct)
    { if (!ModelState.IsValid) return BadRequest("Invalid user update."); await service.UpdateAsync(id, input, ct); TempData["Success"] = "User access updated. Existing sessions were invalidated."; return RedirectToAction(nameof(Index)); }
}
