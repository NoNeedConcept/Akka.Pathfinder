using Akka.Pathfinder.Core.Configs;
using MongoDB.Driver;

namespace Akka.Pathfinder.Core.Services;

public interface IPointConfigReader
{
    public IQueryable<PointConfig> Get(Guid collectionId);

    public Task<PointConfig?> Get(Guid collectionId, int pointId);
}

public class PointConfigReader : IPointConfigReader
{
    protected IMongoDatabase Database { get; init; }
    public PointConfigReader(IMongoDatabase database) => Database = database;
    
    private IMongoCollection<PointConfig> GetCollection(Guid collectionId)
        => Database.GetCollection<PointConfig>(collectionId.ToString());

    public IQueryable<PointConfig> Get(Guid collectionId)
        => GetCollection(collectionId).AsQueryable();

    public async Task<PointConfig?> Get(Guid collectionId, int pointId)
        => (await GetCollection(collectionId).FindAsync(x => x.Id == pointId)).Single();

}
