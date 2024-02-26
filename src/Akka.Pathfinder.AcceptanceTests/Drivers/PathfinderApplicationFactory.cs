using System.Net;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Mvc.Testing;
using Polly;
using Polly.Timeout;
using Serilog;
using static Akka.Pathfinder.Grpc.MapManager;
using static Akka.Pathfinder.Grpc.Pathfinder;

namespace Akka.Pathfinder.AcceptanceTests.Drivers;

public sealed class PathfinderApplicationFactory : WebApplicationFactory<Program>
{
    public PathfinderApplicationFactory()
    {
        Log.Information("[TEST][PathfinderApplicationFactory][ctor]", GetType().Name);

        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
    }

    public async Task InitializeAsync()
    {
        Log.Information("[TEST][PathfinderApplicationFactory][InitializeAsync]");

        var client = CreateClient();
        Log.Information("[TEST][PathfinderApplicationFactory] client ready [{BaseAddress}]",
            client.BaseAddress?.ToString());

        var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(120));
        var isReady = await IsUrlAsync(client, "/health/ready", 20, TimeSpan.FromSeconds(5), TimeSpan.FromMinutes(1), cancellationToken: cts.Token);
        if (!isReady)
        {
            Log.Fatal("[TEST][PathfinderApplicationFactory] application NOT healthy!");
            throw new InvalidOperationException();
        }

        Log.Information("[TEST][PathfinderApplicationFactory] application healthy");
    }

    public override async ValueTask DisposeAsync()
    {
        Log.Information("[TEST][PathfinderApplicationFactory][DisposeAsync]");
        await base.DisposeAsync();
    }

    public GrpcChannel GetGrpcChannel() => GrpcChannel.ForAddress(Server.BaseAddress, new GrpcChannelOptions { HttpHandler = Server.CreateHandler() });

    public MapManagerClient GetMapManagerClient() => new(GetGrpcChannel());

    public PathfinderClient GetPathfinderClient() => new(GetGrpcChannel());

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