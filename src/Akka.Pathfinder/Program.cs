using Akka.Pathfinder;
using Akka.Pathfinder.Startup;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Serilog;
using Servus.Core.Application.Startup;

RegisterMongoShit();
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .MinimumLevel.Debug()
    .CreateBootstrapLogger();

var runner = AppBuilder.Create(WebApplication.CreateBuilder(args), b =>
    {
        b.WebHost.ConfigureKestrel(options => options.ConfigureEndpointDefaults(listenOptions => listenOptions.Protocols = HttpProtocols.Http1AndHttp2));
        return b.Build();
    })
    .WithSetup<ConfigurationSetupContaine>()
    .WithSetup<LoggingSetupContainer>()
    .WithSetup<ServiceSetupContainer>()
    .WithSetup<AkkaStartupContainer>()
    .WithSetup<OpenTelemetryContainer>()
    .WithSetup<HostConfigurationSetupContainer>();

await runner.Build().RunAsync();

public partial class Program
{
    private static int _registered;

    protected Program()
    {
    }

    private static void RegisterMongoShit()
    {
        if (Interlocked.Increment(ref _registered) == 1)
            BsonShit.Register();
    }
}