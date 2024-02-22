using Akka.Pathfinder.Core;
using Akka.Pathfinder.Startup;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Serilog;
using Servus.Core.Application.Startup;

namespace Akka.Pathfinder;

public class Program
{
    public static async Task Main(string[] args)
    {
        var registered = 0;
        if (Interlocked.Increment(ref registered) == 1 && Environment.GetEnvironmentVariable("TESTING") != "1")
        {
            BsonShit.Register();
        }

        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .MinimumLevel.Debug()
            .CreateBootstrapLogger();

        var runner = AppBuilder.Create(WebApplication.CreateBuilder(args), b =>
            {
                b.WebHost.ConfigureKestrel(options => options.ConfigureEndpointDefaults(listenOptions => listenOptions.Protocols = HttpProtocols.Http1AndHttp2));
                return b.Build();
            })
            .WithSetup<ConfigurationSetupContainer>()
            .WithSetup<LoggingSetupContainer>()
            .WithSetup<ServiceSetupContainer>()
            .WithSetup<AkkaStartupContainer>()
            .WithSetup<OpenTelemetryContainer>()
            .WithSetup<HostConfigurationSetupContainer>();

        await runner.Build().RunAsync();
    }
}