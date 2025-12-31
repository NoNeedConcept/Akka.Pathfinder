using Akka.Pathfinder.Core.Messages;
using Akka.Pathfinder.Core.States;
using Akka.Actor;
using Akka.Persistence;
using Akka.Pathfinder.Core.Persistence.Data;
using Akka.Cluster.Sharding;

namespace Akka.Pathfinder.Workers;

public partial class PathfinderWorker
{
    public void PathfinderRequestHandler(PathfinderRequest msg)
    {
        using var activity = Telemetry.ActivitySource.StartActivity(msg.GetType().Name)!.SetTag("EntityId", _entityId);
        _logger.Debug("[{PathfinderId}][{MessageType}] received", _entityId, msg.GetType().Name);

        _state = PathfinderWorkerState.FromRequest(msg);
        _senderManagerClient.Forward(new SavePathfinderSender(msg.PathfinderId));

        IReadOnlyList<PathPoint> startPointList =
        [
            new(_state.SourcePointId, 0, _state.StartDirection)
        ];

        var findPathRequest = new FindPathRequest(msg.RequestId, msg.PathfinderId, Guid.NewGuid(), _state.SourcePointId, _state.TargetPointId, startPointList);
        _mapManagerClient.Tell(findPathRequest, Self);
        Timers!.StartSingleTimer("timeout", new Timeout(msg.RequestId, _state.PathfinderId), _state.Timeout);
        SnapshotState();
    }

    public void FoundPathHandler(PathFound msg)
    {
        using var activity = Telemetry.ActivitySource.StartActivity(msg.GetType().Name)!.SetTag("EntityId", _entityId);
        _logger.Verbose("[{PathfinderId}][{MessageType}] received", _entityId, msg.GetType().Name);

        switch (msg.Result)
        {
            case PathfinderResult.Success:
                _logger.Information("[{PathfinderId}] I found a path [{PathId}][{TotalSeconds}]", _entityId, msg.PathId, (DateTime.UtcNow - _state.StartTime).TotalSeconds);
                _state.IncrementFoundPathCounter();
                break;
        }
    }

    public void TimeoutHandler(Timeout msg)
    {
        using var activity = Telemetry.ActivitySource.StartActivity(msg.GetType().Name)!.SetTag("EntityId", _entityId);
        _logger.Verbose("[{PathfinderId}][{MessageType}] received", _entityId, msg.GetType().Name);

        if (!_state.HasPathFound)
        {
            _logger.Debug("[{PathfinderId}] No Paths found for Path: [{SourcePointId}] -> [{TargetPointId}]", _entityId, _state.SourcePointId, _state.TargetPointId);
            ForwardToPathfinderSender(new PathfinderResponse(msg.RequestId, msg.PathfinderId, false, null, "Frag mich doch nicht"));
            _logger.Information("[{PathfinderId}] -> job is done.... 🦥 mode activated", _entityId);
            Become(Void);
            Shutdown();
            return;
        }

        _logger.Information("[{PathfinderId}] {PathsCount} Paths found for Path: [{SourcePointId}] -> [{TargetPointId}]", _entityId, _state.Count, _state.SourcePointId, _state.TargetPointId);

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
            _logger.Error(ex, "[{PathfinderId}] -> Exception: {@Exception}", _entityId, ex);
            ForwardToPathfinderSender(new PathfinderResponse(msg.RequestId, msg.PathfinderId, false, null, ex.Message));
        }
        finally
        {
            _logger.Information("[{PathfinderId}] -> job is done.... 🦥 mode activated", _entityId);
            Become(Void);
            Shutdown();
        }
    }

    private void Shutdown()
    {
        Context.System.EventStream.Publish(new PathfinderDeactivated(_state.PathfinderId));
        Context.Parent.Tell(new Passivate(PoisonPill.Instance));
    }
    private void ForwardToPathfinderSender(PathfinderResponse message) => _senderManagerClient.Tell(new ForwardToPathfinderSender(message.PathfinderId, message));

    private void SaveSnapshotFailureHandler(SaveSnapshotFailure msg)
        => _logger.Error(msg.Cause, "[{PointId}][SNAPSHOTFAILURE][{SequenceNr}]", _entityId, msg.Metadata.SequenceNr);

    private void SaveSnapshotSuccessHandler(SaveSnapshotSuccess msg)
        => _logger.Debug("[{PointId}][SNAPSHOTSUCESS][{SequenceNr}]", _entityId, msg.Metadata.SequenceNr);
}