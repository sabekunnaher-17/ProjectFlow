using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectFlow.Core.Common;
using ProjectFlow.Core.DTOs;
using ProjectFlow.Infrastructure.Services;

namespace ProjectFlow.Web.Controllers;

[Authorize]
public sealed class TasksController(TaskService service, ProjectService projects, CommentService comments) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(TaskQuery input, bool board, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest("Invalid task filters.");
        ViewData["Query"] = input;
        ViewData["Board"] = board;
        if (input.ProjectId.HasValue)
            ViewData["Project"] = await projects.GetAsync(input.ProjectId.Value, ct);
        return View(await service.ListAsync(input, ct));
    }
    [HttpGet]
    public async Task<IActionResult> Details(Guid id, int commentPage = 1, CancellationToken ct = default)
    {
        if (commentPage is < 1 or > 1000000)
            return BadRequest();
        ViewData["Comments"] = await comments.ListAsync(id, commentPage, ct);
        return View(await service.GetAsync(id, ct));
    }
    [HttpPost]
    public async Task<IActionResult> Comment(Guid id, CommentRequest input, CancellationToken ct)
    {
        if (ModelState.IsValid)
        {
            await comments.AddAsync(id, input, ct);
            return RedirectToAction(nameof(Details), new
            {
                id
            });
        }
        ViewData["SubmittedBody"] = input.Body;
        ViewData["Comments"] = await comments.ListAsync(id, 1, ct);
        return View("Details", await service.GetAsync(id, ct));
    }
    [HttpGet]
    public async Task<IActionResult> Create(Guid projectId, CancellationToken ct)
    {
        var p = await projects.GetAsync(projectId, ct);
        if (!p.CanManage)
            return Forbid();
        ViewData["Project"] = p;
        return View("Editor", new TaskRequest { ProjectId = p.Id, StartDate = p.StartDate, DueDate = p.EndDate });
    }
    [HttpPost]
    public async Task<IActionResult> Create(TaskRequest input, CancellationToken ct)
    {
        if (ModelState.IsValid)
        {
            try
            {
                return RedirectToAction(nameof(Details), new
                {
                    id = await service.CreateAsync(input, ct)
                });
            }
            catch (DomainException e) when (e.Kind == FailureKind.Validation) { ModelState.AddModelError("", e.Message); }
        }
        ViewData["Project"] = await projects.GetAsync(input.ProjectId, ct);
        return View("Editor", input);
    }
    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
    {
        var task = await service.GetAsync(id, ct);
        if (!task.CanManage)
            return Forbid();
        ViewData["Id"] = id;
        ViewData["Project"] = await projects.GetAsync(task.ProjectId, ct);
        return View("Editor", new TaskRequest { ProjectId = task.ProjectId, Title = task.Title, Description = task.Description, AssigneeId = task.AssigneeId, Priority = task.Priority, StartDate = task.StartDate, DueDate = task.DueDate, Version = task.Version });
    }
    [HttpPost]
    public async Task<IActionResult> Edit(Guid id, TaskRequest input, CancellationToken ct)
    {
        if (ModelState.IsValid)
        {
            try
            {
                await service.UpdateAsync(id, input, ct);
                return RedirectToAction(nameof(Details), new
                {
                    id
                });
            }
            catch (DomainException e) when (e.Kind == FailureKind.Validation) { ModelState.AddModelError("", e.Message); }
        }
        ViewData["Id"] = id;
        ViewData["Project"] = await projects.GetAsync(input.ProjectId, ct);
        return View("Editor", input);
    }
    [HttpPost]
    public async Task<IActionResult> Status(Guid id, StatusRequest input, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest("Choose a valid status.");
        await service.StatusAsync(id, input, ct);
        return RedirectToAction(nameof(Details), new
        {
            id
        });
    }
}
