using Akka.Pathfinder.Core.Services;
using Akka.Pathfinder.Core.Messages;
using Akka.Pathfinder.Core.Configs;
using Akka.Pathfinder.Core.States;
using Akka.Persistence;
using Akka.Actor;

namespace Akka.Pathfinder.Workers;

public record LocalPointConfig(PointConfig Config);

public partial class PointWorker : ReceivePersistentActor
{
    public override string PersistenceId => $"PointWorker_{EntityId}";
    public string EntityId;

    private readonly IPointConfigReader _pointConfigReader;
    private readonly IPathWriter _pathWriter;
    private readonly Serilog.ILogger _logger = Serilog.Log.Logger.ForContext<PointWorker>();
    private PointWorkerState _state = null!;

    public PointWorker(string entityId, IServiceProvider serviceProvider)
    {
        EntityId = entityId;
        using var scope = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope();
        var provider = scope.ServiceProvider;
        _pointConfigReader = provider.GetRequiredService<IPointConfigReader>();
        _pathWriter = provider.GetRequiredService<IPathWriter>();

        var result = Context.System.EventStream.Subscribe(Self, typeof(PathfinderDeactivated));
        if (!result)
        {
            _logger.Error("[{PointId}] Subscribe [PathfinderDeactivated] failed", entityId);
        }

        Recover<SnapshotOffer>(RecoverSnapshotOffer);
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

    private void OnConfigure()
    {
        _pointConfigReader
        .Get(_state.CollectionId, _state.PointId)
        .PipeTo(Self, Self, config => config is not null ? new LocalPointConfig(config) : new LocalPointConfig(null!));
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