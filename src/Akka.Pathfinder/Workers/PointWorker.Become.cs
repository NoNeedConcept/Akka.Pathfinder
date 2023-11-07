using Akka.Pathfinder.Core.Messages;
using Akka.Persistence;

namespace Akka.Pathfinder.Workers;

public partial class PointWorker
{
    private void Configure()
    {
        _logger.Debug("[{PointId}][CONFIFURE]", EntityId);

        Command<LocalPointConfig>(LocalPointConfigHandler);
        CommandAny(msg => Stash.Stash());
    }

    private void Failure()
    {
        _logger.Debug("[{PointId}][FAILURE]", EntityId);
        CommandAny(msg =>
        {
            _logger.Debug("[{PointId}][{MessageType}] message received -> no action in failure state", EntityId, msg.GetType().Name);
        });
    }

    private void Ready()
    {
        _logger.Debug("[{PointId}][READY]", EntityId);

        // Sender -> PathfinderWorker
        CommandAsync<FindPathRequest>(CreatePathPointRequestPathHandler);
        Command<PathfinderDeactivated>(PathfinderDeactivatedHandler);
        // Sender -> MapManager 
        Command<CostRequest>(CostRequestHandler);
        Command<PointCommandRequest>(PointCommandRequestHandler);
        Command<InitializePoint>(InitializePointHandler);
        Command<UpdatePointDirection>(UpdatePointDirectionHandler);
        Command<ResetPoint>(ResetPointHandler);
        // Sender -> SnapshotStore
        Command<SaveSnapshotSuccess>(SaveSnapshotSuccessHandler);
        Command<SaveSnapshotFailure>(SaveSnapshotFailureHandler);

        Stash.UnstashAll();
    }
}
