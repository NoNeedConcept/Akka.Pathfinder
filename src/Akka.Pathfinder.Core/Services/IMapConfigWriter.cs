using Akka.Pathfinder.Core.Configs;
using MongoDB.Driver;

namespace Akka.Pathfinder.Core;

public interface IMapConfigWriter : IMapConfigReader
{
    bool AddOrUpdate(Guid Id, MapConfig config);
}

public class MapConfigWriter : MapConfigReader, IMapConfigWriter
{
    public MapConfigWriter(IMongoCollection<MapConfig> collection) : base(collection)
    { }

    public bool AddOrUpdate(Guid Id, MapConfig config) => Collection.ReplaceOne(x => x.Id == Id, config, new ReplaceOptions() { IsUpsert = true }).IsAcknowledged;
}
