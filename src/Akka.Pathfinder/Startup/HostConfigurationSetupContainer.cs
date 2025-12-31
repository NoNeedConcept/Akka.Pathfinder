using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Servus.Core.Application.Startup;

namespace Akka.Pathfinder.Startup;

public class HostConfigurationSetupContainer : ApplicationSetupContainer<WebApplication>
{
    protected override void SetupApplication(WebApplication app)
    {
        app.MapGrpcService<MapManagerService>();
        app.MapGrpcService<PathfinderService>();

        app.UseHealthChecks("/health/alive",
            new HealthCheckOptions { AllowCachingResponses = false, Predicate = r => r.Tags.Contains("live") });
        app.UseHealthChecks("/health", new HealthCheckOptions { AllowCachingResponses = false, Predicate = _ => true });
    }
}