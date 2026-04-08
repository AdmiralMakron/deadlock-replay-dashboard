using DeadlockDashboard.Api;
using DeadlockDashboard.Api.Middleware;
using DeadlockDashboard.Web;
using DeadlockDashboard.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Default to port 8080 inside the container; ASPNETCORE_URLS can override.
if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_URLS"))
    && !args.Any(a => a.StartsWith("--urls", StringComparison.OrdinalIgnoreCase)))
{
    builder.WebHost.UseUrls("http://+:8080");
}

// API layer services (controllers, Swagger, parser, stores, queue, worker, CORS).
builder.Services.AddDeadlockDashboardApi();

// Blazor server.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Typed HTTP client for the frontend to call the API (same process — loopback).
// The typed API client talks to this same process over loopback. We construct
// the base address from ASPNETCORE_URLS so tests and dev runs on alternate
// ports still work without code changes.
builder.Services.AddHttpClient<DashboardApiClient>((sp, http) =>
{
    var server = sp.GetRequiredService<Microsoft.AspNetCore.Hosting.Server.IServer>();
    var addrs = server.Features.Get<Microsoft.AspNetCore.Hosting.Server.Features.IServerAddressesFeature>();
    var addr = addrs?.Addresses.FirstOrDefault() ?? "http://localhost:8080";
    // Prefer http loopback if https is the only bound URL (self-signed cert issues).
    var uri = new Uri(addr.Replace("+", "localhost").Replace("*", "localhost"));
    if (uri.Scheme == "https")
    {
        uri = new Uri($"http://localhost:{uri.Port}");
    }
    http.BaseAddress = uri;
});

var app = builder.Build();

app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<ErrorHandlingMiddleware>();

app.UseStaticFiles();
app.UseRouting();

app.UseCors(ApiServiceCollectionExtensions.CorsPolicy);

app.UseAntiforgery();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Deadlock Replay Dashboard API v1");
    c.RoutePrefix = "swagger";
});

app.MapControllers();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
