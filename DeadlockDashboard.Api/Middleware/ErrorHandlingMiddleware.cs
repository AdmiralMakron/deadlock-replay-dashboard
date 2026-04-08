using System.Text.Json;
using DeadlockDashboard.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace DeadlockDashboard.Api.Middleware;

public sealed class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext ctx)
    {
        try
        {
            await _next(ctx);
        }
        catch (FileNotFoundException ex)
        {
            await WriteError(ctx, StatusCodes.Status404NotFound, "not_found", ex.Message);
        }
        catch (ArgumentException ex)
        {
            await WriteError(ctx, StatusCodes.Status400BadRequest, "bad_request", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception handling {Path}", ctx.Request.Path);
            await WriteError(ctx, StatusCodes.Status500InternalServerError, "internal_error",
                "An unexpected error occurred.", ex.Message);
        }
    }

    private static Task WriteError(HttpContext ctx, int status, string code, string message, string? details = null)
    {
        if (ctx.Response.HasStarted) return Task.CompletedTask;
        ctx.Response.Clear();
        ctx.Response.StatusCode = status;
        ctx.Response.ContentType = "application/json";
        var body = JsonSerializer.Serialize(new ErrorResponseDto(code, message, details));
        return ctx.Response.WriteAsync(body);
    }
}

public sealed class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext ctx)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            await _next(ctx);
        }
        finally
        {
            sw.Stop();
            if (ctx.Request.Path.StartsWithSegments("/api"))
            {
                _logger.LogInformation("{Method} {Path} -> {Status} in {Ms}ms",
                    ctx.Request.Method, ctx.Request.Path, ctx.Response.StatusCode, sw.ElapsedMilliseconds);
            }
        }
    }
}
