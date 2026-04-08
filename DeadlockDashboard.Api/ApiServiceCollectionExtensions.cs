using DeadlockDashboard.Api.Services;
using DeadlockDashboard.Api.Stores;
using DeadlockDashboard.Core.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;

namespace DeadlockDashboard.Api;

public static class ApiServiceCollectionExtensions
{
    public const string CorsPolicy = "DeadlockDashboardCors";

    public static IServiceCollection AddDeadlockDashboardApi(this IServiceCollection services)
    {
        services.AddSingleton<IDemoParserService, DemoParserService>();
        services.AddSingleton<MatchStore>();
        services.AddSingleton<JobStore>();
        services.AddSingleton<DemoDirectory>();
        services.AddSingleton<ParseJobQueue>();
        services.AddSingleton<IParseJobQueue>(sp => sp.GetRequiredService<ParseJobQueue>());
        services.AddHostedService<ParseJobWorker>();

        services.AddControllers()
            .AddApplicationPart(typeof(ApiServiceCollectionExtensions).Assembly);

        services.AddCors(o => o.AddPolicy(CorsPolicy, p =>
            p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Deadlock Replay Dashboard API",
                Version = "v1",
                Description = "REST API for parsing and querying Deadlock post-match data.",
            });
            var xmlFile = System.IO.Path.Combine(AppContext.BaseDirectory, "DeadlockDashboard.Api.xml");
            if (System.IO.File.Exists(xmlFile))
                c.IncludeXmlComments(xmlFile);
        });

        return services;
    }
}
