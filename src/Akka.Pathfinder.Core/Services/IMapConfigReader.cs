using Akka.Pathfinder.Core.Configs;
using MongoDB.Driver;

namespace Akka.Pathfinder.Core;

public interface IMapConfigReader
{
    IQueryable<MapConfig> Get();
    IQueryable<PointConfig> Get(Guid MapId);
    IQueryable<PointConfig> GetPointWithChanges(Guid MapId);
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

    public IQueryable<PointConfig> Get(Guid MapId) 
    => Get().Single(x => x.Id == MapId).PointConfigsIds.SelectMany(x => Database.GetCollection<PointConfig>(x.ToString()).AsQueryable()).AsQueryable();

    public IQueryable<PointConfig> GetPointWithChanges(Guid MapId) => Get(MapId).Where(x => x.HasChanges);
}