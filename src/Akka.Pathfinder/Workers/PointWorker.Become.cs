using Akka.Pathfinder.Core.Messages;
using Akka.Cluster.Sharding;
using Akka.Persistence;
using Akka.Actor;

namespace Akka.Pathfinder.Workers;

public partial class PointWorker
{
    private void Initialize()
    {
        _logger.Debug("[{PointId}][INITIALIZE]", EntityId);
        Command<InitializePoint>(InitializePointHandler);
        Command<ReceiveTimeout>(msg => Context.Parent.Tell(new Passivate(PoisonPill.Instance)));
        CommandAny(msg => Stash.Stash());
    }

    private void Configure()
    {
        _logger.Debug("[{PointId}][CONFIGURE]", EntityId);
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
        Command<UpdatePointDirection>(UpdatePointDirectionHandler);
        Command<ResetPoint>(ResetPointHandler);
        // Sender -> SnapshotStore
        Command<SaveSnapshotSuccess>(SaveSnapshotSuccessHandler);
        Command<SaveSnapshotFailure>(SaveSnapshotFailureHandler);
        Command<ReceiveTimeout>(msg => Context.Parent.Tell(new Passivate(PoisonPill.Instance)));

        Stash.UnstashAll();
    }
}
