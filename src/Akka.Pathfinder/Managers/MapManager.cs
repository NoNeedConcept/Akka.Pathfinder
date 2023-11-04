using Akka.Actor;
using Akka.Pathfinder.Core.States;
using Akka.Persistence;

namespace Akka.Pathfinder.Managers;

public partial class MapManager : ReceivePersistentActor
{
    public override string PersistenceId => $"MapManager";

    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly Serilog.ILogger _logger = Serilog.Log.Logger.ForContext<MapManager>();
    private MapManagerState _state = new(new Dictionary<Guid, Guid>());
    private readonly IActorRef _pointWorkerClient = ActorRefs.Nobody;

    public MapManager(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
        Ready();
    }
}