using Akka.Pathfinder.Core.Configs;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Akka.Pathfinder.Core;

public interface IPointConfigReader
{
    public IMongoQueryable<PointConfig> Get(Guid collectionId);

    public Task<PointConfig?> Get(Guid collectionId, int pointId, CancellationToken cancellationToken = default);
}

public class PointConfigReader : IPointConfigReader
{
    protected IMongoDatabase Database { get; init; }
    public PointConfigReader(IMongoDatabase database) 
        => Database = database;
    
    private IMongoCollection<PointConfig> GetCollection(Guid collectionId)
        => Database.GetCollection<PointConfig>(collectionId.ToString());

    public IMongoQueryable<PointConfig> Get(Guid collectionId)
        => GetCollection(collectionId).AsQueryable();

    public async Task<PointConfig?> Get(Guid collectionId, int pointId, CancellationToken cancellationToken = default)
        => await GetCollection(collectionId).AsQueryable().Where(x => x.Id == pointId).SingleOrDefaultAsync(cancellationToken);
}
