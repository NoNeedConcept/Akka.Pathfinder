using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Servus.Core.Application.Startup;

namespace Akka.Pathfinder.Startup;

public class HostConfigurationSetupContainer : ApplicationSetupContainer<WebApplication>
{
    protected override void SetupApplication(WebApplication app)
    {
        app.MapGrpcService<MapManagerService>();
        app.MapGrpcService<PathfinderService>();
        app.UseHealthChecks("/health/ready", new HealthCheckOptions { AllowCachingResponses = false, Predicate = _ => true });
    }
}