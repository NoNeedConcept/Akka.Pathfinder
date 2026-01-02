using MongoDB.Driver;
using Path = Akka.Pathfinder.Core.Persistence.Data.Path;

namespace Akka.Pathfinder.Core;

public interface IPathReader
{
    IQueryable<Path> Get();
    IQueryable<Path> Get(Guid id);
    long GetPathCost(Guid id);
    IEnumerable<Path> GetByPathfinderId(Guid pathfinderId);
}

public class PathReader : IPathReader
{
    public PathReader(IMongoCollection<Path> collection)
        => Collection = collection;

    protected IMongoCollection<Path> Collection { get; }

    public IQueryable<Path> Get()
        => Collection.AsQueryable();
    public IQueryable<Path> Get(Guid id)
        => Get().Where(x => x.Id == id);
    public long GetPathCost(Guid id)
        => Get(id).SelectMany(x => x.Directions).Sum(x => x.Cost);
    public IEnumerable<Path> GetByPathfinderId(Guid pathfinderId)
        => Get().Where(x => x.PathfinderId == pathfinderId).ToList();
}
