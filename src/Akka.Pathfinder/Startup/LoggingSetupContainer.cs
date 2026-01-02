using Serilog;
using Servus.Core.Application.Startup;

namespace Akka.Pathfinder.Startup;

public class LoggingSetupContainer : IHostBuilderSetupContainer
{
    public void ConfigureHostBuilder(IHostBuilder builder)
    {
        builder
            .UseSerilog((context, _, configuration) =>
            {
                configuration.ReadFrom.Configuration(context.Configuration).Enrich.FromLogContext();
                var endpoint = context.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
                if (string.IsNullOrWhiteSpace(endpoint)) return;
                configuration.WriteTo.OpenTelemetry();
            });
    }
}