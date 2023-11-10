using System.Collections.Concurrent;

namespace Akka.Pathfinder.Core;

public class BufferWorkerState
{
    private readonly int _max;
    private readonly ConcurrentDictionary<Guid, List<int>> _pathfinderPoints = new();

    public BufferWorkerState(int maxPoints) => _max = maxPoints;
    public bool AddPointForPathfinder(Guid pathfinderId, int pointId)
    {
        if (_pathfinderPoints.TryGetValue(pathfinderId, out var points))
        {
            return _pathfinderPoints.TryUpdate(pathfinderId, new List<int>(points) { pointId }.Distinct().ToList(), points);
        }
        else
        {
            return _pathfinderPoints.TryAdd(pathfinderId, new() { pointId });
        }
    }

    public bool HasAllPointsArrived(Guid pathfinderId) => _pathfinderPoints.TryGetValue(pathfinderId, out var points) && points.Count == _max;
}
