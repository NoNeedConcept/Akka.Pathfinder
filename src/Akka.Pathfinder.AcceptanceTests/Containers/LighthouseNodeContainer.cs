using System.Runtime.InteropServices;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Serilog;

namespace Akka.Pathfinder.AcceptanceTests.Containers;

public sealed class LighthouseNodeContainer : IAsyncLifetime
{
    public const int Port = 42000;
    public const string Hostname = "127.0.0.1";

    public LighthouseNodeContainer(string actorSystem = "Zeus")
    {
        Log.Information("[TEST][{LighthouseNodeContainer}] ctor", GetType().Name);

        // PetaBridge uses different tag for arm64
        var dockerTag = "latest";
        if (RuntimeInformation.OSArchitecture == Architecture.Arm64)
            dockerTag = "arm64-latest";

        Container = new ContainerBuilder()
            .WithName($"lighthouse_{Guid.NewGuid():D}")
            .WithImage($"petabridge/lighthouse:{dockerTag}")
            .WithAutoRemove(true)
            .WithHostname("host.docker.internal")
            .WithPortBinding(9110, 9110)
            .WithPortBinding(Port, Port)
            .WithEnvironment("ACTORSYSTEM", $"{actorSystem}")
            .WithEnvironment("CLUSTER_PORT", $"{Port}")
            .WithEnvironment("CLUSTER_IP", $"{Hostname}")
            .WithEnvironment("CLUSTER_SEEDS", $"akka.tcp://{actorSystem}@{Hostname}:{Port}")
            .WithWaitStrategy(Wait.ForUnixContainer()
                .AddCustomWaitStrategy(new WaitUntilLogMessage("Started up successfully")))
            .Build();
    }

    public IContainer Container { get; }

    public async Task InitializeAsync()
    {
        Log.Information("[TEST][{LighthouseNodeContainer}] InitializeAsync", GetType().Name);

        var timeoutCts = new CancellationTokenSource();
        timeoutCts.CancelAfter(TimeSpan.FromMinutes(1));
        await Container.StartAsync(timeoutCts.Token).ConfigureAwait(false);

        Log.Information("[TEST][{LighthouseNodeContainer}] started and ready on Port [{Hostname}:{PublicPort}]",
            GetType().Name, Hostname, Port);

        Log.Information("[TEST][{LighthouseNodeContainer}] InitializeAsync finished", GetType().Name);
    }

    public async Task DisposeAsync()
    {
        Log.Information("[TEST][{LighthouseNodeContainer}] DisposeAsync", GetType().Name);
        await Container.StopAsync();
        await Container.DisposeAsync();
        Log.Information("[TEST][{LighthouseNodeContainer}] DisposeAsync finished", GetType().Name);
    }
}

public sealed class WaitUntilLogMessage : IWaitUntil
{
    private static readonly string[] LineEndings = { "\r\n", "\n" };
    private readonly string _logMessage;

    public WaitUntilLogMessage(string logMessage)
    {
        _logMessage = logMessage ?? throw new ArgumentNullException(nameof(logMessage));
    }

    public async Task<bool> UntilAsync(IContainer container)
    {
        var (stdout, stderr) = await container
            .GetLogsAsync(timestampsEnabled: false)
            .ConfigureAwait(false);

        var concatLog = stdout.Split(LineEndings, StringSplitOptions.RemoveEmptyEntries)
            .Concat(stderr.Split(LineEndings, StringSplitOptions.RemoveEmptyEntries));
        return concatLog.Any(line => line.Contains(_logMessage));
    }
}