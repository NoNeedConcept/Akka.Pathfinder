namespace Akka.Pathfinder.Workers;

public partial class PathfinderWorker
{
    private void PersistState()
    {
        var persistedWorkerState = _state.GetPersistenceState();
        SaveSnapshot(persistedWorkerState);
    }
}
