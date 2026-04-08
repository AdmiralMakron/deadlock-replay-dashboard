using Microsoft.Extensions.Configuration;

namespace DeadlockDashboard.Api.Services;

public sealed class DemoDirectory
{
    public string Path { get; }

    public DemoDirectory(IConfiguration config)
    {
        Path = config["Demos:Directory"] ?? "/app/demos";
    }
}
