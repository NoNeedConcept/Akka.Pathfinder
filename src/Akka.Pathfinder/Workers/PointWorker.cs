using Akka.Pathfinder.Core.Messages;
using Akka.Pathfinder.Core.Configs;
using Akka.Pathfinder.Core.States;
using Akka.Persistence;
using Akka.Actor;
using Akka.Pathfinder.Core;
using Akka.Pathfinder.Core.Persistence;

namespace Akka.Pathfinder.Workers;

public abstract record LocalPointConfig(PointConfig? Config = default);

public record LocalPointConfigSuccess(PointConfig Config) : LocalPointConfig(Config);
public record LocalPointConfigFailed(Exception Exception) : LocalPointConfig;

public partial class PointWorker : ReceivePersistentActor
{
    public override string PersistenceId => $"PointWorker_{_entityId}";
    private string _entityId;
    private readonly IPointConfigReader _pointConfigReader;
    private readonly IPathWriter _pathWriter;
    private readonly Serilog.ILogger _logger;
    private PointWorkerState _state = null!;

    public PointWorker(string entityId, IServiceProvider serviceProvider)
    {
        _entityId = entityId;
        _logger = Serilog.Log.Logger.ForContext("SourceContext", GetType().Name);
        var provider = serviceProvider;
        _pointConfigReader = provider.GetRequiredService<IPointConfigReader>();
        _pathWriter = provider.GetRequiredService<IPathWriter>();

        var result = Context.System.EventStream.Subscribe(Self, typeof(PathfinderDeactivated));
        if (!result)
        {
            _logger.Error("[{PointId}] Subscribe [PathfinderDeactivated] failed", entityId);
        }
        Recover<PersistedInitializedPointState>(state => _state = PointWorkerState.FromPersistedPointState(state));
        Recover<SnapshotOffer>(RecoverSnapshotOffer);
        CommandAny(msg => Stash.Stash());
    }

    protected override void OnReplaySuccess()
    {
        _logger.Verbose("[{PointId}][RECOVER] SUCCESS", _entityId);
        if (_state is not null && _state.Loaded)
        {
            _logger.Verbose("[{PointId}][RECOVER] Ready", _entityId);
            Become(Ready);
        }
        else
        {
            _logger.Verbose("[{PointId}][RECOVER] Initialize", _entityId);
            Become(Initialize);
        }
    }

    private void OnConfigure()
    {
        _logger.Verbose("[{PointId}][RECOVER] Initialize", _entityId);

        _pointConfigReader
        .Get(_state.CollectionId, _state.PointId)
        .PipeTo(Self, Self, config => config is not null ? new LocalPointConfigSuccess(config) : new LocalPointConfigFailed(null!), ex => new LocalPointConfigFailed(ex));
    }

    protected override void PreRestart(Exception reason, object message)
        => _logger.Error("[{PointId}] PreRestart(): [{RestartReason}] on [{MessageType}][{MessageData}]",
            _entityId, reason.Message, message.GetType().Name, message);

    protected override void OnPersistFailure(Exception cause, object @event, long sequenceNr)
        => _logger.Error("[{PointId}] OnPersistFailure(): [{ExceptionMessage}] on [{MessageType}][{MessageData}][{SeqNr}]",
            _entityId, cause.Message, @event.GetType().Name, @event, sequenceNr);

    protected override void OnPersistRejected(Exception cause, object @event, long sequenceNr)
        => _logger.Error("[{PointId}] OnPersistRejected(): [{ExceptionMessage}] on [{MessageType}][{MessageData}][{SeqNr}]",
            _entityId, cause.Message, @event.GetType().Name, @event, sequenceNr);

}