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

                var useOtlpExporter = !string.IsNullOrWhiteSpace(context.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);
                if (!useOtlpExporter) return;
                configuration.WriteTo.OpenTelemetry();
            });
    }

}