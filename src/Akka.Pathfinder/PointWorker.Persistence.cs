using Akka.Pathfinder.Core.Services;

namespace Akka.Pathfinder;

public partial class PointWorker
{
    private void PersistState()
    {
        var persistedWorkerState = _state.GetPersistenceState();
        SaveSnapshot(persistedWorkerState);
    }

    private bool PersistPath(Core.Persistence.Data.Path path)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var pathWriter = scope.ServiceProvider.GetRequiredService<IPathWriter>();
        return pathWriter.AddOrUpdate(path);
    }
}
