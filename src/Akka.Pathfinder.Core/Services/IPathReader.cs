using MongoDB.Driver;
using Path = Akka.Pathfinder.Core.Persistence.Data.Path;

namespace Akka.Pathfinder.Core.Services;

public interface IPathReader
{
    IQueryable<Path> Get();
    IQueryable<Path> Get(Guid id);
    Task<IEnumerable<Path>> GetByPathfinderIdAsync(Guid pathfinderId, CancellationToken cancellationToken = default);

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
}
