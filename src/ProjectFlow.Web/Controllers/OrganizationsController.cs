using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectFlow.Core.Common;
using ProjectFlow.Core.DTOs;
using ProjectFlow.Infrastructure.Services;

namespace ProjectFlow.Web.Controllers;

[Authorize]
public sealed class OrganizationsController(OrganizationService service) : Controller
{
    [HttpGet] public async Task<IActionResult> Index(CancellationToken ct) => View(await service.ListAsync(ct));
    [HttpGet] public async Task<IActionResult> Details(Guid id, CancellationToken ct) => View(await service.GetAsync(id, ct));
    [HttpPost]
    public async Task<IActionResult> Create(OrganizationRequest input, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View("Index", await service.ListAsync(ct));
        return RedirectToAction(nameof(Details), new
        {
            id = await service.CreateAsync(input, ct)
        });
    }
    [HttpPost]
    public async Task<IActionResult> AddMember(Guid id, OrganizationMemberRequest input, CancellationToken ct)
    {
        if (ModelState.IsValid)
        {
            try
            {
                await service.AddMemberAsync(id, input, ct);
                return RedirectToAction(nameof(Details), new
                {
                    id
                });
            }
            catch (DomainException e) when (e.Kind == FailureKind.Validation) { ModelState.AddModelError("", e.Message); }
        }
        return View("Details", await service.GetAsync(id, ct));
    }
}
