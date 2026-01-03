using Akka.Hosting;
using Akka.Pathfinder.Core;
using Akka.Pathfinder.Core.States;
using Akka.Persistence;
using moin.akka.endpoint;
using Servus.Core.Diagnostics;

namespace Akka.Pathfinder.Managers;

[ActivitySourceName("Pathfinder")]
public partial class MapManager : ReceivePersistentActor
{
    public override string PersistenceId => "MapManager";
    private readonly Serilog.ILogger _logger;
    private readonly IReadOnlyActorRegistry _registry;
    private readonly IMapConfigWriter _mapConfigWriter;
    private readonly IPointConfigWriter _pointConfigWriter;
    private MapManagerState _state = new();

    public MapManager(IServiceScopeFactory serviceScopeFactory)
    {
        _logger = Serilog.Log.Logger.ForContext("SourceContext", GetType().Name);
        using var scope = serviceScopeFactory.CreateScope();
        var provider = scope.ServiceProvider;
        _mapConfigWriter = provider.GetRequiredService<IMapConfigWriter>();
        _pointConfigWriter = provider.GetRequiredService<IPointConfigWriter>();
        _registry = Context.System.GetRegistry();

        Recover<SnapshotOffer>(RecoverSnapshotOffer);
        CommandAny(msg => Stash.Stash());
    }

    protected override void OnReplaySuccess()
    {
        _logger.Information("[MapManager][RECOVER] SUCCESS");
        Become(Ready);
    }

    protected override void PreRestart(Exception reason, object message)
        => _logger.Error("[MapManager] PreRestart(): [{RestartReason}] on [{MessageType}][{MessageData}]",
            reason.Message, message.GetType().Name, message);

    protected override void OnPersistFailure(Exception cause, object @event, long sequenceNr)
        => _logger.Error(
            "[MapManager] OnPersistFailure(): [{ExceptionMessage}] on [{MessageType}][{MessageData}][{SeqNr}]",
            cause.Message, @event.GetType().Name, @event, sequenceNr);

    protected override void OnPersistRejected(Exception cause, object @event, long sequenceNr)
        => _logger.Error(
            "[MapManager] OnPersistRejected(): [{ExceptionMessage}] on [{MessageType}][{MessageData}][{SeqNr}]",
            cause.Message, @event.GetType().Name, @event, sequenceNr);
}