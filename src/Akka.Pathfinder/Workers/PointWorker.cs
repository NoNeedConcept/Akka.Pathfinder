using System.Diagnostics;
using Akka.Pathfinder.Core.Messages;
using Akka.Pathfinder.Core.Configs;
using Akka.Pathfinder.Core.States;
using Akka.Persistence;
using Akka.Actor;
using Akka.Pathfinder.Core;
using Akka.Pathfinder.Core.Persistence;
using Akka.Pathfinder.Core.Services;
using Servus.Core.Diagnostics;

namespace Akka.Pathfinder.Workers;

public abstract record LocalPointConfig(PointConfig? Config = null) : IWithTracing
{
    public string? TraceId { get; set; }
    public string? SpanId { get; set; }
}

public record LocalPointConfigSuccess(PointConfig Config) : LocalPointConfig(Config);

public record LocalPointConfigFailed(Exception Exception) : LocalPointConfig;

[ActivitySourceName("Pathfinder")]
public partial class PointWorker : ReceivePersistentActor
{
    public override string PersistenceId => $"PointWorker_{_entityId}";
    private readonly string _entityId;
    private readonly IPointConfigReader _pointConfigReader;
    private readonly IPathWriter _pathWriter;
    private readonly Serilog.ILogger _logger;
    private PointWorkerState _state = null!;

    public PointWorker(string entityId, IServiceProvider serviceProvider)
    {
        _entityId = entityId;
        _logger = Serilog.Log.Logger.ForContext("SourceContext", GetType().Name);
        _pointConfigReader = serviceProvider.GetRequiredService<IPointConfigReader>();
        _pathWriter = serviceProvider.GetRequiredService<IPathWriter>();
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
            Become(Ready);
        }
        else if (_state is not null && _state.Initialized)
        {
            Become(Configure);
        }
        else
        {
            Become(Initialize);
        }
    }

    private void OnConfigure()
    {
        var traceId = Activity.Current?.TraceId.ToHexString();
        var spanId = Activity.Current?.SpanId.ToHexString();

        _pointConfigReader
            .Get(_state.CollectionId, _state.PointId)
            .PipeTo(Self, Sender, config =>
            {
                IWithTracing result = new LocalPointConfigFailed(null!);
                if (config is not null)
                {
                    result = new LocalPointConfigSuccess(config);
                }

                return result.WithTracing(traceId, spanId);
            }, ex => new LocalPointConfigFailed(ex).WithTracing(traceId, spanId));
    }

    protected override void OnRecoveryFailure(Exception reason, object message = null)
        => _logger.Error("[{PointId}] OnRecoveryFailure(): [{RestartReason}] on [{MessageType}][{@MessageData}]",
            _entityId, reason.Message, message.GetType().Name, message);

    protected override void PreRestart(Exception reason, object message)
        => _logger.Error("[{PointId}] PreRestart(): [{RestartReason}] on [{MessageType}][{MessageData}]",
            _entityId, reason.Message, message.GetType().Name, message);

    protected override void OnPersistFailure(Exception cause, object @event, long sequenceNr)
        => _logger.Error(
            "[{PointId}] OnPersistFailure(): [{ExceptionMessage}] on [{MessageType}][{MessageData}][{SeqNr}]",
            _entityId, cause.Message, @event.GetType().Name, @event, sequenceNr);

    protected override void OnPersistRejected(Exception cause, object @event, long sequenceNr)
        => _logger.Error(
            "[{PointId}] OnPersistRejected(): [{ExceptionMessage}] on [{MessageType}][{MessageData}][{SeqNr}]",
            _entityId, cause.Message, @event.GetType().Name, @event, sequenceNr);
    
}