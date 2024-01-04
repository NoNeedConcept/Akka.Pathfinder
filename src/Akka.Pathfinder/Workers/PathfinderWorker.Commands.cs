using Akka.Pathfinder.Core.Messages;
using Akka.Pathfinder.Core.States;
using Akka.Actor;
using LanguageExt;
using Akka.Persistence;

namespace Akka.Pathfinder.Workers;

public partial class PathfinderWorker
{
    public void FindPathHandler(PathfinderStartRequest msg)
    {
        _logger.Verbose("[{PathfinderId}][{MessageType}] received", EntityId, msg.GetType().Name);

        _state = PathfinderWorkerState.FromRequest(msg);
        _senderManagerClient.Forward(new SavePathfinderSender(msg.PathfinderId));

        IReadOnlyList<PathPoint> startPointList = new List<PathPoint>()
        {
            new(_state.SourcePointId, 0, _state.StartDirection)
        };

        var findPathRequest = new FindPathRequest(Guid.Parse(EntityId), Guid.NewGuid(), _state.SourcePointId, _state.TargetPointId, startPointList);
        _mapManagerClient.Tell(findPathRequest, Self);
        PersistState();
    }

    private void FindPathRequestStarted(FindPathRequestStarted msg)
    {
        _logger.Verbose("[{PathfinderId}][{MessageType}] received", EntityId, msg.GetType().Name);
        Context.System.Scheduler.ScheduleTellOnce(_state.Timeout, Self, new PathfinderTimeout(_state.PathfinderId), Self);
    }

    public void FoundPathHandler(PathFound msg)
    {
        _logger.Verbose("[{PathfinderId}][{MessageType}] received", EntityId, msg.GetType().Name);

        switch (msg.Result)
        {
            case PathfinderResult.Success:
                _state.IncrementFoundPathCounter();
                break;
            default:
                _logger.Debug("[{PathfinderId}] Jan wanted a log here with the reason {Result}", msg.PathfinderId, msg.Result);
                break;
        }
    }

    public async Task PathfinderTimeoutHandler(PathfinderTimeout msg)
    {
        _logger.Verbose("[{PathfinderId}][{MessageType}] received", EntityId, msg.GetType().Name);
        Become(WhilePathEvaluation);
        Context.System.EventStream.Publish(new PathfinderDeactivated(_state.PathfinderId));

        if (!_state.HasPathFound)
        {
            _logger.Debug("[{PathfinderId}] No Paths found for Path: [{SourcePointId}] -> [{TargetPointId}]", EntityId, _state.SourcePointId, _state.TargetPointId);
            _senderManagerClient.Tell(new ForwardToPathfinderSender(msg.PathfinderId, new PathFinderDone(msg.PathfinderId, Guid.Empty, false, "Frag mich doch nicht")));
            Become(Void);
            Stash.UnstashAll();
            return;
        }

        _logger.Debug("[{PathfinderId}] {PathsCount} Paths found for Path: [{SourcePointId}] -> [{TargetPointId}]", EntityId, _state.Count, _state.SourcePointId, _state.TargetPointId);

        await _pathReader
        .GetByPathfinderIdAsync(msg.PathfinderId)
        .PipeTo(Self, Self,
        result =>
        {
            var paths = result.ToList();
            var pathsOrderedByCost = paths.OrderByDescending(p => p.Directions.Select(x => (int)x.Cost).Sum()).Last();
            var bestPathId = pathsOrderedByCost.Id;
            return new BestPathFound(msg.PathfinderId, bestPathId);
        },
        ex => new BestPathFailed(msg.PathfinderId, ex));
    }

    public void BestPathFoundHandler(BestPathFound msg)
    {
        _logger.Verbose("[{PathfinderId}][{MessageType}] received", EntityId, msg.GetType().Name);
        Become(Void);
        Stash.UnstashAll();
        _senderManagerClient.Tell(new ForwardToPathfinderSender(msg.PathfinderId, new PathFinderDone(msg.PathfinderId, msg.PathId, true)));
    }

    public void BestPathFailedHandler(BestPathFailed msg)
    {
        _logger.Verbose("[{PathfinderId}][{MessageType}] received", EntityId, msg.GetType().Name);
        Become(Void);
        Stash.UnstashAll();
        if (msg.Exception is not null)
        {
            _logger.Error(msg.Exception, "[{PathfinderId}] -> Exception: {@Exception}", EntityId, msg.Exception);
        }

        Sender.Tell(new PathFinderDone(msg.PathfinderId, Guid.Empty, false, "Frag mich doch nicht"));
    }

    private void SaveSnapshotFailureHandler(SaveSnapshotFailure msg)
    => _logger.Error("[{PathfinderId}] failed to create snapshot [{SequenceNr}]",
            EntityId, msg.Metadata.SequenceNr);

    private void SaveSnapshotSuccessHandler(SaveSnapshotSuccess msg)
        => _logger.Information("[{PathfinderId}] successfully create snapshot [{SequenceNr}]",
                EntityId, msg.Metadata.SequenceNr);
}