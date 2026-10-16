using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectFlow.Core.Common;

namespace ProjectFlow.Web.Security;

public sealed class ExceptionHandler(ILogger<ExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception error, CancellationToken ct)
    {
        var status = error switch
        {
            DomainException d => d.Kind switch { FailureKind.NotFound => 404, FailureKind.Forbidden => 403, FailureKind.Conflict => 409, _ => 400 },
            DbUpdateConcurrencyException => 409,
            DbUpdateException => 409,
            _ => 500
        };
        // Do not log arbitrary exception/SQL values or connection strings.
        if (status == 500)
            logger.LogError("Request failed with type {Type}; trace {Trace}", error.GetType().Name, context.TraceIdentifier);
        context.Response.StatusCode = status;
        var message = error is DomainException ? error.Message : status == 409 ? "Another change was saved. Reload and try again." : "The request could not be completed. Please try again later.";
        if (context.Request.Path.StartsWithSegments("/api"))
            await context.Response.WriteAsJsonAsync(new ProblemDetails { Status = status, Title = message, Extensions = { ["traceId"] = context.TraceIdentifier } }, ct);
        else
        {
            context.Response.ContentType = "text/html; charset=utf-8";
            await context.Response.WriteAsync($"<!doctype html><html lang=\"en\"><title>ProjectFlow — Request unavailable</title><h1>Request unavailable</h1><p>{System.Net.WebUtility.HtmlEncode(message)}</p><a href=\"/\">Return home</a></html>", ct);
        }
        return true;
    }
}
