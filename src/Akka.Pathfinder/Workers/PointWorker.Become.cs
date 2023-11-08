using Akka.Actor;
using Akka.Cluster.Sharding;
using Akka.Pathfinder.Core.Messages;
using Akka.Persistence;

namespace Akka.Pathfinder.Workers;

public partial class PointWorker
{
    private void Initialize()
    {
        Command<InitializePoint>(InitializePointHandler);
        Command<ReceiveTimeout>(msg => Context.Parent.Tell(new Passivate(PoisonPill.Instance)));
    }

    private void Configure()
    {
        _logger.Debug("[{PointId}][CONFIFURE]", EntityId);

        Command<LocalPointConfig>(LocalPointConfigHandler);
        CommandAny(msg => Stash.Stash());
    }

    private void Failure()
    {
        _logger.Debug("[{PointId}][FAILURE]", EntityId);
        Command<ReceiveTimeout>(msg => Context.Parent.Tell(new Passivate(PoisonPill.Instance)));
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
        Command<ReceiveTimeout>(msg => Context.Parent.Tell(new Passivate(PoisonPill.Instance)));

        Stash.UnstashAll();
    }
}
