using System.Net;
using Akka.Pathfinder.Grpc;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Polly;
using Polly.Timeout;
using Serilog;

namespace Akka.Pathfinder.AcceptanceTests.Drivers;

public class GrpcApplicationFactory : WebApplicationFactory<Akka.Pathfinder.Grpc.Program>
{
    public GrpcApplicationFactory()
    {
        Log.Information("[TEST][GrpcApplicationFactory][ctor]");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        //builder.UseEnvironment("Production");
        builder.UseEnvironment("Development");
        builder.UseSetting("akka:remote:dot-netty:tcp:port", PortFinder.FindFreeLocalPort().ToString());
    }

    public async Task InitializeAsync()
    {
        Log.Information("[TEST][GrpcApplicationFactory][InitializeAsync]");
        var client = CreateClient();
        Log.Information("[TEST][GrpcApplicationFactory] client ready [{BaseAddress}]",
            client.BaseAddress?.ToString());

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(120));
        var isReady = await IsUrlAsync(client, "/health", 20, TimeSpan.FromSeconds(5), TimeSpan.FromMinutes(1),
            cancellationToken: cts.Token);
        if (!isReady)
        {
            Log.Fatal("[TEST][GrpcApplicationFactory] application NOT healthy!");
            throw new InvalidOperationException();
        }

        Log.Information("[TEST][GrpcApplicationFactory] application healthy");
    }

    public override async ValueTask DisposeAsync()
    {
        Log.Information("[TEST][GrpcApplicationFactory][DisposeAsync]");
        await base.DisposeAsync();
    }

    public GrpcChannel GetGrpcChannel() => GrpcChannel.ForAddress(Server.BaseAddress,
        new GrpcChannelOptions { HttpHandler = Server.CreateHandler() });

    public MapManager.MapManagerClient GetMapManagerClient() => new(GetGrpcChannel());

    public Grpc.Pathfinder.PathfinderClient GetPathfinderClient() => new(GetGrpcChannel());

    public static async Task<bool> IsUrlAsync(HttpClient client, string relativeUri, int retry = 5,
        TimeSpan? delay = null, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        delay ??= TimeSpan.FromSeconds(1);
        timeout ??= TimeSpan.FromMinutes(5);

        client.Timeout = timeout.Value;

        var overallTimeoutPolicy = Policy.TimeoutAsync(timeout.Value);
        var customPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                retry,
                _ => delay.Value,
                (ex, span, index, _) =>
                {
                    Log.Error("[Polly][{Retries}] Request failed. Trying again in {TimeSpan} seconds.", index, span);
                });

        var policy = overallTimeoutPolicy.WrapAsync(customPolicy);

        try
        {
            return await policy.ExecuteAsync(async () =>
            {
                var response = await client.GetAsync(relativeUri, cancellationToken);
                return response.StatusCode switch
                {
                    HttpStatusCode.OK => true,
                    HttpStatusCode.ServiceUnavailable => throw new Exception("MOin"),
                    _ => throw new Exception("MOin")
                };
            });
        }
        catch (TimeoutRejectedException)
        {
            return false;
        }
    }
}