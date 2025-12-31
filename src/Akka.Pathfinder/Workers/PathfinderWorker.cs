using Akka.Pathfinder.Core.States;
using Akka.Pathfinder.Core;
using Akka.Persistence;
using Akka.Actor;
using moin.akka.endpoint;

namespace Akka.Pathfinder.Workers;

public record Timeout(Guid RequestId, Guid PathfinderId) : IPathfinderId;

public record TimeoutPathFound(Guid RequestId, Guid PathfinderId, Guid PathId) : IPathfinderId;

public record TimeoutPathFailed(Guid RequestId, Guid PathfinderId, Exception? Exception = default) : IPathfinderId;

public partial class PathfinderWorker : ReceivePersistentActor, IWithTimers
{
    private string _entityId;

    private readonly IPathReader _pathReader;
    private readonly IActorRef _mapManagerClient;
    private readonly IActorRef _senderManagerClient;
    private readonly Serilog.ILogger _logger;
    private PathfinderWorkerState _state = null!;

    public PathfinderWorker(string entityId, IServiceScopeFactory serviceScopeFactory)
    {
        _entityId = entityId;
        _logger = Serilog.Log.Logger.ForContext("SourceContext", GetType().Name);
        using var scope = serviceScopeFactory.CreateScope();
        var provider = scope.ServiceProvider;
        _pathReader = provider.GetRequiredService<IPathReader>();

        var registry = Context.System.GetRegistry();
        _mapManagerClient = registry.GetClient<Endpoint.MapManager>();
        _senderManagerClient = registry.GetClient<Endpoint.SenderManager>();

        Recover<SnapshotOffer>(RecoverSnapshotOffer);
        CommandAny(msg => Stash.Stash());
    }

    public override string PersistenceId => $"PathfinderWorker_{_entityId}";

    public ITimerScheduler? Timers { get; set; }

    protected override void OnReplaySuccess()
    {
        _logger.Information("[{PathfinderId}][RECOVER] SUCCESS", _entityId);
        Become(Ready);
    }

    protected override void PreRestart(Exception reason, object message)
        => _logger.Error("[{PathfinderId}] PreRestart(): [{RestartReason}] on [{MessageType}][{MessageData}]",
            _entityId, reason.Message, message.GetType().Name, message);

    protected override void OnPersistFailure(Exception cause, object @event, long sequenceNr)
        => _logger.Error(
            "[{PathfinderId}] OnPersistFailure(): [{ExceptionMessage}] on [{MessageType}][{MessageData}][{SeqNr}]",
            _entityId, cause.Message, @event.GetType().Name, @event, sequenceNr);

    protected override void OnPersistRejected(Exception cause, object @event, long sequenceNr)
        => _logger.Error(
            "[{PathfinderId}] OnPersistRejected(): [{ExceptionMessage}] on [{MessageType}][{MessageData}][{SeqNr}]",
            _entityId, cause.Message, @event.GetType().Name, @event, sequenceNr);
}