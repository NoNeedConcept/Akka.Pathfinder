using Akka.Pathfinder.Core;
using Akka.Persistence;

namespace Akka.Pathfinder;

public partial class BufferWorker : ReceivePersistentActor
{
    public override string PersistenceId => $"BufferWorker_{EntityId}";
    public string EntityId;
    private readonly Serilog.ILogger _logger = Serilog.Log.Logger.ForContext<BufferWorker>();
    private readonly IMapConfigReader _mapConfigReader;
    private BufferWorkerState _state = null!;

    public BufferWorker(string entityId, IServiceScopeFactory serviceScopeFactory)
    {
        EntityId = entityId;
        using var scope = serviceScopeFactory.CreateScope();
        var provider = scope.ServiceProvider;
        _mapConfigReader = provider.GetRequiredService<IMapConfigReader>();
    }
}
