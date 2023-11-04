using System.Collections.Concurrent;
using Akka.Actor;
using Akka.Persistence;

namespace Akka.Pathfinder.Managers;

public partial class MapManager : ReceivePersistentActor
{
    public override string PersistenceId => $"MapManager";
    
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly Serilog.ILogger _logger = Serilog.Log.Logger.ForContext<MapManager>();
    private readonly IActorRef _pointWorkerClient =  ActorRefs.Nobody;
    private readonly ConcurrentDictionary<int, (DateTime Created, DateTime? Completed)> _readyPoints = new();
    private readonly ConcurrentBag<Guid> _waitingPathfinders = new();

    public MapManager(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }
}