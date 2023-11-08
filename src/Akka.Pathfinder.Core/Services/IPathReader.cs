using System.Security.Cryptography.X509Certificates;
using MongoDB.Driver;
using Path = Akka.Pathfinder.Core.Persistence.Data.Path;

namespace Akka.Pathfinder.Core.Services;

public interface IPathReader
{
    IQueryable<Path> Get();
    IQueryable<Path> Get(Guid id);
    Task<IEnumerable<Path>> GetByPathfinderIdAsync(Guid pathfinderId, CancellationToken cancellationToken = default);

    long GetPathCost(Guid id);
}

public class PathReader : IPathReader
{
    private IMongoCollection<Path> _mongoCollection { get; set; }

    public PathReader(IMongoCollection<Path> collection)
    {
        _mongoCollection = collection;
    }

    public IQueryable<Path> Get() => _mongoCollection.AsQueryable();

    public IQueryable<Path> Get(Guid id) => Get().Where(x => x.Id == id);

    public async Task<IEnumerable<Path>> GetByPathfinderIdAsync(Guid pathfinderId, CancellationToken cancellationToken = default) => (await _mongoCollection.FindAsync(x => x.PathfinderId == pathfinderId, cancellationToken: cancellationToken)).ToEnumerable(cancellationToken: cancellationToken);

    public long GetPathCost(Guid id) => Get(id).SelectMany(x => x.Directions).Sum(x => x.Cost);
}
