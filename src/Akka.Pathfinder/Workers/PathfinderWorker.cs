using Akka.Pathfinder.Core.Messages;
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

        Command<PathfinderStartRequest>(FindPathHandler);
        Command<FindPathRequestStarted>(FindPathRequestStarted);
        Command<PathFound>(FoundPathHandler);
        CommandAsync<PathfinderTimeout>(PathfinderTimeoutHandler);
    }
 }