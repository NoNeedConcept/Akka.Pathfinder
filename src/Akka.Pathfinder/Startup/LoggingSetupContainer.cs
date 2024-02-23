using Serilog;
using Servus.Core.Application.Startup;

namespace Akka.Pathfinder.Startup;

public class LoggingSetupContainer : ApplicationSetupContainer<WebApplication>,ILoggingSetupContainer, IServiceSetupContainer
{
    public void SetupLogging(ILoggingBuilder builder)
    {
        builder
            .ClearProviders()
            .AddSerilog();
    }

    public void SetupServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSerilog();
    }

    protected override void SetupApplication(WebApplication app)
    {
        app.UseSerilogRequestLogging();
    }
}