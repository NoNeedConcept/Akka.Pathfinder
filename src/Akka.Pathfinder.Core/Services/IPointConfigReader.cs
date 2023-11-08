using Akka.Pathfinder.Core.Configs;
using MongoDB.Driver;

namespace Akka.Pathfinder.Core.Services;

public interface IPointConfigReader
{
    public IQueryable<PointConfig> Get(Guid CollectionId);

    public IQueryable<PointConfig> Get(Guid CollectionId, int pointId);
}

public class PointConfigReader : IPointConfigReader
{
    protected IMongoDatabase Database { get; init; }
    public PointConfigReader(IMongoDatabase database)
    {
        Database = database;
    }

    public IQueryable<PointConfig> Get(Guid CollectionId) => Database.GetCollection<PointConfig>(CollectionId.ToString()).AsQueryable();

    public IQueryable<PointConfig> Get(Guid CollectionId, int pointId) => Get(CollectionId).Where(x => x.Id == pointId);

}
