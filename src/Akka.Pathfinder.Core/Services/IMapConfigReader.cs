using Akka.Pathfinder.Core.Configs;
using MongoDB.Driver;

namespace Akka.Pathfinder.Core;

public interface IMapConfigReader
{
    IQueryable<MapConfig> Get();
    IEnumerable<PointConfig> Get(Guid MapId);
}

public class MapConfigReader : IMapConfigReader
{
    protected IMongoCollection<MapConfig> Collection { get; init; }
    protected IMongoDatabase Database { get; init; }
    public MapConfigReader(IMongoCollection<MapConfig> collection, IMongoDatabase database) 
    {
        Collection = collection;
        Database = database;
    }

    public IQueryable<MapConfig> Get() => Collection.AsQueryable();

    public IEnumerable<PointConfig> Get(Guid MapId) => Database.GetCollection<PointConfig>(Get().Single(x => x.Id == MapId).PointConfigsId.ToString()).AsQueryable();
}