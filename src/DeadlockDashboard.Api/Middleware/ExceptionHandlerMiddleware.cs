using System.Net;
using System.Text.Json;
using DeadlockDashboard.Shared.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace DeadlockDashboard.Api.Middleware;

public class ExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlerMiddleware> _logger;

    public ExceptionHandlerMiddleware(RequestDelegate next, ILogger<ExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception on {Method} {Path}", context.Request.Method, context.Request.Path);

            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";

            var error = new ErrorResponse
            {
                Error = "INTERNAL_ERROR",
                Message = "An unexpected error occurred.",
                Details = null
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(error, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }));
        }
    }
}
