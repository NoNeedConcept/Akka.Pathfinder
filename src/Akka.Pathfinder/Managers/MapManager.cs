using Akka.Pathfinder.Core;
using Akka.Pathfinder.Core.Services;
using Akka.Pathfinder.Core.States;
using Akka.Persistence;

namespace Akka.Pathfinder.Managers;

public partial class MapManager : ReceivePersistentActor
{
    public override string PersistenceId => $"MapManager";
    private readonly Serilog.ILogger _logger = Serilog.Log.Logger.ForContext<MapManager>();

    private IMapConfigReader _mapConfigReader;
    private IPointConfigReader _pointConfigReader;
    private MapManagerState _state = new(new Dictionary<Guid, Guid>());

    public MapManager(IServiceScopeFactory serviceScopeFactory)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var provider = scope.ServiceProvider;
        _mapConfigReader = provider.GetRequiredService<IMapConfigReader>();
        _pointConfigReader = provider.GetRequiredService<IPointConfigReader>();
        Ready();
    }
}