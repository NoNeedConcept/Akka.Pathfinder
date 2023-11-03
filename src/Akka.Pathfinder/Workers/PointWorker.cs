using Akka.Actor;
using Akka.Pathfinder.Core;
using Akka.Pathfinder.Core.Configs;
using Akka.Pathfinder.Core.Messages;
using Akka.Pathfinder.Core.Services;
using Akka.Persistence;

namespace Akka.Pathfinder.Workers;

public partial class PointWorker : ReceivePersistentActor
{
    public override string PersistenceId => $"PointWorker_{EntityId}";
    public string EntityId;
    private PointWorkerState _state = null!;

    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly Serilog.ILogger _logger = Serilog.Log.Logger.ForContext<PointWorker>();
    private readonly IActorRef _pointWorkerClient = ActorRefs.Nobody;
    private readonly IActorRef _pathfinderClient = ActorRefs.Nobody;

    public PointWorker(string entityId, IServiceProvider serviceProvider)
    {
        EntityId = entityId;
        _serviceScopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
        _pointWorkerClient = Context.System.GetRegistry().Get<PointWorkerProxy>();

        var result = Context.System.EventStream.Subscribe(Self, typeof(PathfinderDeactivated));
        if (!result)
        {
            _logger.Error("[{PointId}] Subscribe [PathfinderDeactivated] failed", entityId);
        }

        Recover<SnapshotOffer>(RecoverSnapshotOffer);
        Recover<PointConfig>(RecoverPointConfig);
        CommandAny(msg => Stash.Stash());
    }

    protected override void OnReplaySuccess()
    {
        _logger.Debug("[{PointId}][RECOVER] SUCCESS", EntityId);

        if (_state?.Initialize == true)
        {
            Become(Ready);
        }
        else
        {
            Become(Initialize);
        }
    }

    protected void OnInitialize()
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var configReader = scope.ServiceProvider
            .GetRequiredService<IPointConfigReader>();

        var config = configReader.Get(Convert.ToInt32(EntityId)).SingleOrDefault();

        if (config is null)
        {
            _logger.Error("[{PointId}] failed to query point config from database", EntityId);
            return;
        }

        Self.Tell(config);
    }

    private void OnReady()
    {
        Command<PathfinderDeactivated>(PathfinderDeactivatedHandler);
        Command<CostRequest>(CostRequestHandler);
        Command<PointCommandRequest>(PointCommandRequestHandler);
        Command<FindPathRequest>(CreatePathPointRequestPathHandler);

        Command<SaveSnapshotSuccess>(SaveSnapshotSuccessHandler);
        Command<SaveSnapshotFailure>(SaveSnapshotFailureHandler);
    }

    protected override void PreRestart(Exception reason, object message)
        => _logger.Error("[{PointId}] PreRestart(): [{RestartReason}] on [{MessageType}][{MessageData}]",
            EntityId, reason.Message, message.GetType().Name, message);

    protected override void OnPersistFailure(Exception cause, object @event, long sequenceNr)
        => _logger.Error("[{PointId}] OnPersistFailure(): [{ExceptionMessage}] on [{MessageType}][{MessageData}][{SeqNr}]",
            EntityId, cause.Message, @event.GetType().Name, @event, sequenceNr);

    protected override void OnPersistRejected(Exception cause, object @event, long sequenceNr)
        => _logger.Error("[{PointId}] OnPersistRejected(): [{ExceptionMessage}] on [{MessageType}][{MessageData}][{SeqNr}]",
            EntityId, cause.Message, @event.GetType().Name, @event, sequenceNr);
}