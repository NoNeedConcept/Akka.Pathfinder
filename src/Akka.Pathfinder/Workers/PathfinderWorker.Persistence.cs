namespace Akka.Pathfinder.Workers;

public partial class PathfinderWorker
{
    private void SnapshotState()
    {
        var persistedWorkerState = _state.GetPersistenceState();
        SaveSnapshot(persistedWorkerState);
    }
}
