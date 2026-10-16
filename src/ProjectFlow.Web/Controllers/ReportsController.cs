using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectFlow.Core.DTOs;
using ProjectFlow.Infrastructure.Services;

namespace ProjectFlow.Web.Controllers;

[Authorize]
public sealed class ReportsController(TaskService tasks, InsightService insights) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(TaskQuery input, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest("Invalid report filters.");
        ViewData["Query"] = input;
        return View(await tasks.ListAsync(input, ct));
    }
    [HttpGet]
    public async Task<IActionResult> Export(TaskQuery input, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest("Invalid report filters.");
        return File(System.Text.Encoding.UTF8.GetBytes(await insights.ExportAsync(input, ct)), "text/csv; charset=utf-8", "ProjectFlow-tasks.csv");
    }
}
