using Akka.Pathfinder.Core.States;
using Akka.Pathfinder.Core;
using Akka.Persistence;
using Akka.Actor;
using Akka.Pathfinder.Core.Services;

namespace Akka.Pathfinder.Workers;

public partial class PathfinderWorker : ReceivePersistentActor
{
    public override string PersistenceId => $"PathfinderWorker_{EntityId}";
    public string EntityId;

    private readonly IPathReader _pathReader;
    private readonly IActorRef _mapManagerClient = ActorRefs.Nobody;
    private readonly IActorRef _senderManagerClient = ActorRefs.Nobody;
    private readonly Serilog.ILogger _logger = Serilog.Log.Logger.ForContext<PathfinderWorker>();
    private PathfinderWorkerState _state = null!;
    private IActorRef _sender = ActorRefs.Nobody;


    public PathfinderWorker(string entityId, IServiceScopeFactory serviceScopeFactory)
    {
        EntityId = entityId;
        using var scope = serviceScopeFactory.CreateScope();
        _pathReader = scope.ServiceProvider.GetRequiredService<IPathReader>();

        var registry = Context.System.GetRegistry();
        _mapManagerClient = registry.Get<MapManagerProxy>();
        _senderManagerClient = registry.Get<SenderManagerProxy>();
        
        Recover<SnapshotOffer>(RecoverSnapshotOffer);
        CommandAny(msg => Stash.Stash());
    }

    protected override void OnReplaySuccess()
    {
        _logger.Debug("[{PathfinderId}][RECOVER] SUCCESS", EntityId);
        Become(Ready);
    }

    protected override void PreRestart(Exception reason, object message)
        => _logger.Error("[{PathfinderId}] PreRestart(): [{RestartReason}] on [{MessageType}][{MessageData}]",
            EntityId, reason.Message, message.GetType().Name, message);

    protected override void OnPersistFailure(Exception cause, object @event, long sequenceNr)
        => _logger.Error("[{PathfinderId}] OnPersistFailure(): [{ExceptionMessage}] on [{MessageType}][{MessageData}][{SeqNr}]",
            EntityId, cause.Message, @event.GetType().Name, @event, sequenceNr);

    protected override void OnPersistRejected(Exception cause, object @event, long sequenceNr)
        => _logger.Error("[{PathfinderId}] OnPersistRejected(): [{ExceptionMessage}] on [{MessageType}][{MessageData}][{SeqNr}]",
            EntityId, cause.Message, @event.GetType().Name, @event, sequenceNr);
}