using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Path = Akka.Pathfinder.Core.Persistence.Data.Path;

namespace Akka.Pathfinder.Core.Services;

public interface IPathReader
{
    IMongoQueryable<Path> Get();
    IMongoQueryable<Path> Get(Guid id);
    Task<long> GetPathCostAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Path>> GetByPathfinderIdAsync(Guid pathfinderId, CancellationToken cancellationToken = default);
}

public class PathReader : IPathReader
{
    public PathReader(IMongoCollection<Path> collection)
        => Collection = collection;

    protected IMongoCollection<Path> Collection { get; }

    public IMongoQueryable<Path> Get()
        => Collection.AsQueryable();
    public IMongoQueryable<Path> Get(Guid id)
        => Get().Where(x => x.Id == id);
    public async Task<long> GetPathCostAsync(Guid id, CancellationToken cancellationToken = default)
        => await Get(id).SelectMany(x => x.Directions).SumAsync(x => x.Cost, cancellationToken);
    public async Task<IEnumerable<Path>> GetByPathfinderIdAsync(Guid pathfinderId, CancellationToken cancellationToken = default)
        => await Get().Where(x => x.PathfinderId == pathfinderId).ToListAsync(cancellationToken);
}
