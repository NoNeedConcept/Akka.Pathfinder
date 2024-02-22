using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Polly;
using Polly.Timeout;
using Serilog;

namespace Akka.Pathfinder.AcceptanceTests.Drivers;

public sealed class PathfinderApplicationFactory : WebApplicationFactory<Program>
{
    public PathfinderApplicationFactory()
    {
        Log.Information("[TEST][PathfinderApplicationFactory][ctor]");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        //builder.UseEnvironment("Production");
        builder.UseEnvironment("Development");
        builder.UseSetting("akka:remote:dot-netty:tcp:port", PortFinder.FindFreeLocalPort().ToString());
    }

    public async Task InitializeAsync()
    {
        Log.Information("[TEST][PathfinderApplicationFactory][InitializeAsync]");
        var client = CreateClient();
        Log.Information("[TEST][PathfinderApplicationFactory] client ready [{BaseAddress}]",
            client.BaseAddress?.ToString());

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(120));
        var isReady = await IsUrlAsync(client, "/health", 20, TimeSpan.FromSeconds(5), TimeSpan.FromMinutes(1),
            cancellationToken: cts.Token);
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

public class PortFinder
{
    /// <summary>
    /// Returns a unused local port on the current host
    /// </summary>
    /// <returns>Port number or 0 if no free port was found.</returns>
    public static int FindFreeLocalPort()
    {
        int port;
        var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        try
        {
            var endpoint = new IPEndPoint(IPAddress.Any, 0);
            socket.Bind(endpoint);
            endpoint = (IPEndPoint)socket.LocalEndPoint!;
            port = endpoint.Port;
        }
        finally
        {
            socket.Close();
        }

        return port;
    }
}