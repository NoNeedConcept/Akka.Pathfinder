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

    public bool AddOrUpdate(Guid Id, MapConfig config)
    {
        var updater = new UpdateDefinitionBuilder<MapConfig>()
        .Set(x => x.PointConfigsIds, config.PointConfigsIds)
        .Set(x => x.Count, config.Count);       
        return Collection.UpdateOne(x => x.Id == Id, updater, new UpdateOptions() { IsUpsert = true }).IsAcknowledged;
    }
}
