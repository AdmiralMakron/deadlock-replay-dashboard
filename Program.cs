using DeadlockDashboard.Core.Services;
using DeadlockDashboard.Web.Services;
using DeadlockDashboard.Components;
using DeadlockDashboard.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Core services (singletons)
builder.Services.AddSingleton<JobStore>();
builder.Services.AddSingleton<MatchStore>();
builder.Services.AddSingleton<ParseJobQueue>();
builder.Services.AddSingleton<DemoParserService>();
builder.Services.AddHostedService<ParseJobBackgroundService>();

// API controllers + Swagger
builder.Services.AddControllers()
    .AddApplicationPart(typeof(DeadlockDashboard.Api.Controllers.DemosController).Assembly);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opts =>
{
    opts.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Deadlock Dashboard API",
        Version = "v1",
        Description = "REST API for parsing and analyzing Deadlock replay demo files."
    });

    // Include XML doc comments from all projects
    var xmlFiles = Directory.GetFiles(AppContext.BaseDirectory, "*.xml", SearchOption.TopDirectoryOnly);
    foreach (var xmlFile in xmlFiles)
    {
        opts.IncludeXmlComments(xmlFile);
    }
});

// CORS
builder.Services.AddCors(opts =>
{
    opts.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

// Blazor
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Typed HTTP client for Blazor frontend
builder.Services.AddHttpClient<DashboardApiClient>(client =>
{
    client.BaseAddress = new Uri("http://localhost:8080");
});

// Configuration
builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

var app = builder.Build();

// Middleware pipeline
app.UseMiddleware<ExceptionHandlerMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

app.UseCors();

// Swagger
app.UseSwagger();
app.UseSwaggerUI(opts =>
{
    opts.SwaggerEndpoint("/swagger/v1/swagger.json", "Deadlock Dashboard API v1");
    opts.RoutePrefix = "swagger";
});

app.UseAntiforgery();
app.MapStaticAssets();

// Map API controllers
app.MapControllers();

// Map Blazor
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(DeadlockDashboard.Web.Components.Pages.Home).Assembly);

app.Run();
