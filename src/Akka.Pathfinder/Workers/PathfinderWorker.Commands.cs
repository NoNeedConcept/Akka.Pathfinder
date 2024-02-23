using Akka.Pathfinder.Core.Messages;
using Akka.Pathfinder.Core.States;
using Akka.Actor;
using LanguageExt;
using Akka.Persistence;
using Akka.Pathfinder.Core.Persistence.Data;

namespace Akka.Pathfinder.Workers;

public partial class PathfinderWorker
{
    public void PathfinderRequestHandler(PathfinderRequest msg)
    {
        _logger.Verbose("[{PathfinderId}][{MessageType}] received", EntityId, msg.GetType().Name);

        _state = PathfinderWorkerState.FromRequest(msg);
        _senderManagerClient.Forward(new SavePathfinderSender(msg.PathfinderId));

        IReadOnlyList<PathPoint> startPointList =
        [
            new(_state.SourcePointId, 0, _state.StartDirection)
        ];

        var findPathRequest = new FindPathRequest(msg.RequestId, msg.PathfinderId, Guid.NewGuid(), _state.SourcePointId, _state.TargetPointId, startPointList);
        _mapManagerClient.Tell(findPathRequest, Self);
        Context.System.Scheduler.ScheduleTellOnce(_state.Timeout, Self, new Timeout(msg.RequestId, _state.PathfinderId), Self);
        SnapshotState();
    }

    public void FoundPathHandler(PathFound msg)
    {
        _logger.Verbose("[{PathfinderId}][{MessageType}] received", EntityId, msg.GetType().Name);

        switch (msg.Result)
        {
            case PathfinderResult.Success:
                _logger.Information("[{PathfinderId}] I found a path [{PathId}][{TotalSeconds}]", EntityId, msg.PathId, (DateTime.UtcNow - _state.StartTime).TotalSeconds);
                _state.IncrementFoundPathCounter();
                break;
            default:
                _logger.Debug("[{PathfinderId}] Jan wanted a log here with the reason {Result}", EntityId, msg.Result);
                break;
        }
    }

    public void TimeoutHandler(Timeout msg)
    {
        _logger.Verbose("[{PathfinderId}][{MessageType}] received", EntityId, msg.GetType().Name);
        Become(Void);
        Context.System.EventStream.Publish(new PathfinderDeactivated(_state.PathfinderId));

        if (!_state.HasPathFound)
        {
            _logger.Debug("[{PathfinderId}] No Paths found for Path: [{SourcePointId}] -> [{TargetPointId}]", EntityId, _state.SourcePointId, _state.TargetPointId);
            ForwardToPathfinderSender(new PathfinderResponse(msg.RequestId, msg.PathfinderId, false, null, "Frag mich doch nicht"));
            Become(Void);
            Stash.UnstashAll();
            return;
        }

        _logger.Information("[{PathfinderId}] {PathsCount} Paths found for Path: [{SourcePointId}] -> [{TargetPointId}]", EntityId, _state.Count, _state.SourcePointId, _state.TargetPointId);

        var result = _pathReader.GetByPathfinderId(msg.PathfinderId);
        try
        {
            var paths = result.ToList();
            var pathsOrderedByCost = paths.OrderByDescending(p => p.Directions.Select(x => (int)x.Cost).Sum()).Last();
            var bestPathId = pathsOrderedByCost.Id;
            ForwardToPathfinderSender(new PathfinderResponse(msg.RequestId, msg.PathfinderId, true, bestPathId));
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "[{PathfinderId}] -> Exception: {@Exception}", EntityId, ex);
            ForwardToPathfinderSender(new PathfinderResponse(msg.RequestId, msg.PathfinderId, false, null, ex.Message));
        }
        
    }

    private void ForwardToPathfinderSender(PathfinderResponse message) => _senderManagerClient.Tell(new ForwardToPathfinderSender(message.PathfinderId, message));

    private void SaveSnapshotFailureHandler(SaveSnapshotFailure msg)
        => _logger.Error(msg.Cause, "[{PointId}][SNAPSHOTFAILURE][{SequenceNr}]", EntityId, msg.Metadata.SequenceNr);

    private void SaveSnapshotSuccessHandler(SaveSnapshotSuccess msg)
        => _logger.Debug("[{PointId}][SNAPSHOTSUCESS][{SequenceNr}]", EntityId, msg.Metadata.SequenceNr);
}