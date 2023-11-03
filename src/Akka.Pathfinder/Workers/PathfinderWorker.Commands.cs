using Akka.Actor;
using Akka.Pathfinder.Core;
using Akka.Pathfinder.Core.Messages;
using Akka.Pathfinder.Core.Services;

namespace Akka.Pathfinder.Workers;

public partial class PathfinderWorker
{
    public void FindPath(PathfinderStartRequest msg)
    {
        _logger.Debug("[{PathfinderId}][{MessageType}] received", EntityId, msg.GetType().Name);

        _pathfinderWorkerState = PathfinderWorkerState.FromRequest(msg);

        IReadOnlyList<PathPoint> startPointList = new List<PathPoint>()
        {
            new(msg.SourcePointId, 0, msg.Direction)
        };

        var findPathRequest = new FindPathRequest(Guid.Parse(EntityId), Guid.NewGuid(), msg.SourcePointId, msg.TargetPointId, startPointList);

        Context.System.GetRegistry().Get<PointWorkerProxy>().Tell(findPathRequest);

        Context.System.Scheduler.ScheduleTellOnce(msg.Timeout ?? TimeSpan.FromSeconds(30), Self, new FickDichPatrick(_pathfinderWorkerState.PathfinderId), Sender);
    }

    public void FoundPath(PathFound msg)
    {
        _logger.Debug("[{PathfinderId}][{MessageType}] received", EntityId, msg.GetType().Name);

        switch (msg.Result)
        {
            case PathFinderResult.Success:
                _pathfinderWorkerState.IncrementFoundPathCounter();
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
        Context.System.EventStream.Publish(new PathfinderDeactivated(_pathfinderWorkerState.PathfinderId));

        if (!_pathfinderWorkerState.HasPathFound)
        {
            _logger.Debug("[{PathfinderId}] No Paths found for Path: [{SourcePointId}] -> [{TargetPointId}]", EntityId, _pathfinderWorkerState.SourcePointId, _pathfinderWorkerState.TargetPointId);
            Sender.Tell(new PathFinderDone(null));
            Context.SetReceiveTimeout(TimeSpan.FromSeconds(30));
            return;
        }


        _logger.Debug("[{PathfinderId}] {PathsCount} Paths found for Path: [{SourcePointId}] -> [{TargetPointId}]", EntityId, _pathfinderWorkerState.Count, _pathfinderWorkerState.SourcePointId, _pathfinderWorkerState.TargetPointId);
        using var scope = _serviceScopeFactory.CreateScope();
        var pathReader = scope.ServiceProvider.GetRequiredService<IPathReader>();
        await pathReader
        .GetByPathfinderIdAsync(msg.PathfinderId)
        .PipeTo(Self, Sender,
        result =>
        {
            var pathsOrderedByCost = result.OrderByDescending(p => p.Directions.Select(x => (int)x.Cost).Sum());
            var bestPathId = pathsOrderedByCost.Last().Id;
            return new BestPathFound(msg.PathfinderId, bestPathId);
        },
        ex => new BestPathFailed(msg.PathfinderId, ex));
    }

    public void BestPathFoundHandler(BestPathFound msg)
    {
        Become(Void);
        Stash.UnstashAll();
        using var scope = _serviceScopeFactory.CreateScope();
        var pathReader = scope.ServiceProvider.GetRequiredService<IPathReader>();
        var path = pathReader.Get(msg.PathId).SingleOrDefault();
        Sender.Tell(new PathFinderDone(path));
    }

    public void BestPathFailedHandler(BestPathFailed msg)
    {
        Become(Void);
        Stash.UnstashAll();
        if (msg.Exception is not null)
        {
            _logger.Error(msg.Exception, "[{PathfinderId}] -> Exception: {@Exception}", EntityId, msg.Exception);
        }

        Sender.Tell(new PathFinderDone(null));
        Context.SetReceiveTimeout(TimeSpan.FromSeconds(30));
    }
}