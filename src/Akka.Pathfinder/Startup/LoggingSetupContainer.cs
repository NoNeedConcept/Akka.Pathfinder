using Serilog;
using Servus.Core.Application.Startup;

namespace Akka.Pathfinder.Startup;

public class LoggingSetupContainer : IHostBuilderSetupContainer
{
    public void ConfigureHostBuilder(IHostBuilder builder)
    {
        builder.UseSerilog((context, _, configuration) =>
        {
            configuration.ReadFrom.Configuration(context.Configuration).Enrich.FromLogContext();
        });
    }
}