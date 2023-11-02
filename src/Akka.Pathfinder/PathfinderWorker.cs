using Akka.Pathfinder.Core;
using Akka.Pathfinder.Core.Messages;
using Akka.Persistence;

namespace Akka.Pathfinder;

public partial class PathfinderWorker : ReceivePersistentActor
{
    public override string PersistenceId => $"PathfinderWorker_{EntityId}";
    public string EntityId;

    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly Serilog.ILogger _logger = Serilog.Log.Logger.ForContext<PathfinderWorker>();
    private PathfinderWorkerState _pathfinderWorkerState = null!;

    public PathfinderWorker(string entityId, IServiceScopeFactory serviceScopeFactory)
    {
        EntityId = entityId;
        _serviceScopeFactory = serviceScopeFactory;

        Command<PathfinderStartRequest>(FindPath);
        Command<PathFound>(FoundPath);
        CommandAsync<FickDichPatrick>(FickDichPatrick);
    }
}