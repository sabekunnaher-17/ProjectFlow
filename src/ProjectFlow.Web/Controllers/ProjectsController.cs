using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectFlow.Core.Common;
using ProjectFlow.Core.DTOs;
using ProjectFlow.Infrastructure.Services;

namespace ProjectFlow.Web.Controllers;

[Authorize]
public sealed class ProjectsController(ProjectService service, OrganizationService organizations) : Controller
{
    [HttpGet] public async Task<IActionResult> Index(ProjectQuery input, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest("Invalid project filters.");
        ViewData["Query"] = input; return View(await service.ListAsync(input, ct));
    }
    [HttpGet] public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        var p = await service.GetAsync(id, ct);
        if (p.CanManage) ViewData["Organization"] = await organizations.GetAsync(p.OrganizationId, ct);
        return View(p);
    }
    [HttpGet] public async Task<IActionResult> Create(Guid? organizationId, CancellationToken ct)
    {
        ViewData["Organizations"] = await organizations.ListAsync(ct);
        var selected = organizationId ?? ((IReadOnlyList<OrganizationDto>)ViewData["Organizations"]!).FirstOrDefault()?.Id;
        if (!selected.HasValue) return RedirectToAction("Index", "Organizations");
        var org = await organizations.GetAsync(selected.Value, ct); ViewData["Organization"] = org;
        return View("Editor", new ProjectRequest { OrganizationId = org.Id, ManagerId = org.OwnerId });
    }
    [HttpPost] public async Task<IActionResult> Create(ProjectRequest input, CancellationToken ct)
    {
        if (ModelState.IsValid)
        {
            try { return RedirectToAction(nameof(Details), new { id = await service.CreateAsync(input, ct) }); }
            catch (DomainException e) when (e.Kind == FailureKind.Validation) { ModelState.AddModelError("", e.Message); }
        }
        ViewData["Organization"] = await organizations.GetAsync(input.OrganizationId, ct); return View("Editor", input);
    }
    [HttpGet] public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
    {
        var p = await service.GetAsync(id, ct); if (!p.CanManage) return Forbid();
        ViewData["Id"] = id; ViewData["Organization"] = await organizations.GetAsync(p.OrganizationId, ct);
        return View("Editor", new ProjectRequest { OrganizationId = p.OrganizationId, Name = p.Name, Description = p.Description, ManagerId = p.ManagerId, Status = p.Status, StartDate = p.StartDate, EndDate = p.EndDate, Version = p.Version });
    }
    [HttpPost] public async Task<IActionResult> Edit(Guid id, ProjectRequest input, CancellationToken ct)
    {
        if (ModelState.IsValid)
        {
            try { await service.UpdateAsync(id, input, ct); return RedirectToAction(nameof(Details), new { id }); }
            catch (DomainException e) when (e.Kind == FailureKind.Validation) { ModelState.AddModelError("", e.Message); }
        }
        ViewData["Id"] = id; ViewData["Organization"] = await organizations.GetAsync(input.OrganizationId, ct); return View("Editor", input);
    }
    [HttpPost] public async Task<IActionResult> Archive(Guid id, VersionRequest input, CancellationToken ct)
    { await service.ArchiveAsync(id, input.Version, ct); return RedirectToAction(nameof(Details), new { id }); }
    [HttpPost] public async Task<IActionResult> Member(Guid id, MemberRequest input, bool remove, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest("Choose a valid member.");
        await service.MembershipAsync(id, input, remove, ct); return RedirectToAction(nameof(Details), new { id });
    }
}
