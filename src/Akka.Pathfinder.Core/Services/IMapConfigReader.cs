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
    protected readonly IMongoCollection<MapConfig> Collection;
    public MapConfigReader(IMongoCollection<MapConfig> collection) => Collection = collection;

    public IQueryable<MapConfig> Get() => Collection.AsQueryable();

    public IEnumerable<PointConfig> Get(Guid MapId) => Get().First(x => x.Id == MapId).Points;
}