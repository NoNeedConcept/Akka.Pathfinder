using Akka.Pathfinder.Core.Services;

namespace Akka.Pathfinder.Workers;

public partial class PointWorker
{
    private void PersistState()
    {
        var persistedWorkerState = _state.GetPersistenceState();
        SaveSnapshot(persistedWorkerState);
    }

    private (bool Success, Guid PathId) PersistPath(Core.Persistence.Data.Path path)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var pathWriter = scope.ServiceProvider.GetRequiredService<IPathWriter>();
        return (pathWriter.AddOrUpdate(path), path.Id);
    }
}
