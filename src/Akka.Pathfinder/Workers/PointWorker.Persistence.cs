namespace Akka.Pathfinder.Workers;

public partial class PointWorker
{
    private void PersistState()
    {
        var persistedWorkerState = _state.GetPersistenceState();
        SaveSnapshot(persistedWorkerState);
    }

    private (bool Success, Guid PathId) PersistPath(Core.Persistence.Data.Path path)
        => (_pathWriter.AddOrUpdate(path), path.Id);
}
