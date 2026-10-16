using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectFlow.Core.DTOs;
using ProjectFlow.Infrastructure.Services;

namespace ProjectFlow.Web.Controllers.Api;

[ApiController, Route("api/v1"), Authorize]
public sealed class InsightsApiController(InsightService insights, CommentService comments) : ControllerBase
{
    [HttpGet("dashboard")] public Task<DashboardDto> Dashboard(CancellationToken ct) => insights.DashboardAsync(ct);
    [HttpGet("activity")] public Task<PageDto<ActivityDto>> Activity(Guid? projectId, [Range(1, 1000000)] int page = 1, CancellationToken ct = default) => insights.ActivitiesAsync(projectId, page, ct);
    [HttpGet("tasks/{taskId:guid}/comments")] public Task<PageDto<CommentDto>> Comments(Guid taskId, [Range(1, 1000000)] int page = 1, CancellationToken ct = default) => comments.ListAsync(taskId, page, ct);
    [HttpPost("tasks/{taskId:guid}/comments")] public async Task<IActionResult> Comment(Guid taskId, CommentRequest input, CancellationToken ct) { await comments.AddAsync(taskId, input, ct); return StatusCode(201); }
    [HttpGet("reports/tasks.csv")] public async Task<IActionResult> Export([FromQuery] TaskQuery input, CancellationToken ct) => File(System.Text.Encoding.UTF8.GetBytes(await insights.ExportAsync(input, ct)), "text/csv; charset=utf-8", "ProjectFlow-tasks.csv");
}
