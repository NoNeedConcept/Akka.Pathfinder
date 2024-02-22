using Akka.Pathfinder.AcceptanceTests.Drivers;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Reqnroll;
using Reqnroll.BoDi;
using System.Diagnostics;

namespace Akka.Pathfinder.AcceptanceTests.StepDefinitions;

[Binding]
public class ConcurrencySteps
{
    private readonly GrpcApplicationFactory _applicationFactory;
    private readonly Serilog.ILogger _logger = Serilog.Log.Logger.ForContext<ConcurrencySteps>();

    public ConcurrencySteps(ObjectContainer container)
    {
        _applicationFactory = container.Resolve<GrpcApplicationFactory>();
    }

    [When(@"I send (.*) requests simultaneously on one stream with timeout (.*) seconds")]
    public async Task WhenISendRequestsSimultaneously(int count, int seconds)
    {
        var pathfinderClient = _applicationFactory.GetPathfinderClient();
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        var call = pathfinderClient.FindPath(cancellationToken: cts.Token);

        var sw = Stopwatch.StartNew();
        for (var i = 0; i < count; i++)
        {
            var request = new Grpc.FindPathRequest
            {
                PathfinderId = Guid.NewGuid().ToString(),
                Direction = Grpc.Direction.None,
                Duration = Duration.FromTimeSpan(TimeSpan.FromSeconds(seconds)),
                SourcePointId = 2,
                TargetPointId = 3
            };
            await call.RequestStream.WriteAsync(request, cts.Token);
            _logger.Information("[TEST] Sent request {Index}", i + 1);
        }

        await call.RequestStream.CompleteAsync();

        var receivedCount = 0;
        await foreach (var _ in call.ResponseStream.ReadAllAsync(cts.Token))
        {
            receivedCount++;
            _logger.Information("[TEST] Received response {Index} after {Elapsed}ms", receivedCount,
                sw.ElapsedMilliseconds);
        }

        _logger.Information("[TEST] Finished receiving {Count} responses in {Elapsed}ms", receivedCount,
            sw.ElapsedMilliseconds);

        Assert.Equal(count, receivedCount);
        Assert.True(sw.ElapsedMilliseconds < 11000,
            $"Execution took too long: {sw.ElapsedMilliseconds}ms. Expected less than 10000ms.");
    }
}