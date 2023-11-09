using Akka.Pathfinder.Core.Messages;
using Akka.Pathfinder.Core.Services;
using Akka.Pathfinder.Core.States;
using Akka.Pathfinder.Core;
using Akka.Actor;

namespace Akka.Pathfinder.Workers;

public partial class PathfinderWorker
{
    public void FindPath(PathfinderStartRequest msg)
    {
        _logger.Debug("[{PathfinderId}][{MessageType}] received", EntityId, msg.GetType().Name);

        _state = PathfinderWorkerState.FromRequest(msg);
        _sender = Sender;
        _mapManagerClient.Tell(new IsMapReady(msg.PathfinderId));
    }

    public void FoundPath(PathFound msg)
    {
        _logger.Debug("[{PathfinderId}][{MessageType}] received", EntityId, msg.GetType().Name);

        switch (msg.Result)
        {
            case PathFinderResult.Success:
                _state.IncrementFoundPathCounter();
                break;
            default:
                _logger.Debug("[{PathfinderId}] Jan wanted a log here with the reason {Result}", msg.PathfinderId, msg.Result);
                break;
        }
    }

    public async Task FickDichPatrick(FickDichPatrick msg)
    {
        _logger.Debug("[{PathfinderId}][{MessageType}] received", EntityId, msg.GetType().Name);
        Become(WhilePathEvaluation);
        Context.System.EventStream.Publish(new PathfinderDeactivated(_state.PathfinderId));

        if (!_state.HasPathFound)
        {
            _logger.Debug("[{PathfinderId}] No Paths found for Path: [{SourcePointId}] -> [{TargetPointId}]", EntityId, _state.SourcePointId, _state.TargetPointId);
            Sender.Tell(new PathFinderDone(msg.PathfinderId, Guid.Empty, false, "Frag mich doch nicht"));
            Become(Void);
            Stash.UnstashAll();
            return;
        }

        _logger.Debug("[{PathfinderId}] {PathsCount} Paths found for Path: [{SourcePointId}] -> [{TargetPointId}]", EntityId, _state.Count, _state.SourcePointId, _state.TargetPointId);
        using var scope = _serviceScopeFactory.CreateScope();
        var pathReader = scope.ServiceProvider.GetRequiredService<IPathReader>();
        await pathReader
        .GetByPathfinderIdAsync(msg.PathfinderId)
        .PipeTo(Self, Sender,
        result =>
        {
            var paths = result.ToList();
            var pathsOrderedByCost = paths.OrderByDescending(p => p.Directions.Select(x => (int)x.Cost).Sum()).Last();
            var bestPathId = pathsOrderedByCost.Id;
            return new BestPathFound(msg.PathfinderId, bestPathId);
        },
        ex => new BestPathFailed(msg.PathfinderId, ex));
    }

    private void MapIsReadyHandler(MapIsReady msg)
    {
        IReadOnlyList<PathPoint> startPointList = new List<PathPoint>()
        {
            new(_state.SourcePointId, 0, _state.StartDirection)
        };
        var findPathRequest = new FindPathRequest(Guid.Parse(EntityId), Guid.NewGuid(), _state.SourcePointId, _state.TargetPointId, startPointList);

        Context.System.GetRegistry().Get<PointWorkerProxy>().Tell(findPathRequest, Self);

        Context.System.Scheduler.ScheduleTellOnce(_state.Timeout, Self, new FickDichPatrick(_state.PathfinderId), _sender);
    }

    public void BestPathFoundHandler(BestPathFound msg)
    {
        Become(Void);
        Stash.UnstashAll();
        Sender.Tell(new PathFinderDone(msg.PathfinderId, msg.PathId, true));
    }

    public void BestPathFailedHandler(BestPathFailed msg)
    {
        Become(Void);
        Stash.UnstashAll();
        if (msg.Exception is not null)
        {
            _logger.Error(msg.Exception, "[{PathfinderId}] -> Exception: {@Exception}", EntityId, msg.Exception);
        }

        Sender.Tell(new PathFinderDone(msg.PathfinderId, Guid.Empty, false, "Frag mich doch nicht"));
    }
}