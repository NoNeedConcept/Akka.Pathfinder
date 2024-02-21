using Akka.Pathfinder.Core;
using Akka.Pathfinder.Core.Messages;
using Akka.Pathfinder.Core.States;
using Akka.Persistence;

namespace Akka.Pathfinder.Managers;

public partial class MapManager : ReceivePersistentActor
{
    public override string PersistenceId => $"MapManager";
    private readonly Serilog.ILogger _logger = Serilog.Log.Logger.ForContext<MapManager>();
    private readonly IMapConfigReader _mapConfigReader;
    private readonly IPointConfigReader _pointConfigReader;
    private MapManagerState _state = new();

    public MapManager(IServiceScopeFactory serviceScopeFactory)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var provider = scope.ServiceProvider;
        _mapConfigReader = provider.GetRequiredService<IMapConfigReader>();
        _pointConfigReader = provider.GetRequiredService<IPointConfigReader>();
        var registry = Context.System.GetRegistry();

        CommandAsync<LoadMap>(LoadMapHandler);
        CommandAsync<UpdateMap>(UpdateMapHandler);
        Command<FindPathRequest>(FindPathRequestHandler);
        CommandAny(msg => Stash.Stash());
    }
}