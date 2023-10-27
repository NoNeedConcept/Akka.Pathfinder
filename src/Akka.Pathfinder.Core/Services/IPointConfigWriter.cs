using Akka.Pathfinder.Core.Configs;
using MongoDB.Driver;

namespace Akka.Pathfinder.Core.Services;

public interface IPointConfigWriter
{
    public bool AddOrUpdate(PointConfig pointConfig);
}
public class PointConfigWriter : PointConfigReader, IPointConfigWriter
{
    private IMongoCollection<PointConfig> _mongoCollection { get; set; }

    public PointConfigWriter(IMongoCollection<PointConfig> collection) : base(collection)
    {
        _mongoCollection = collection;
    }

    public bool AddOrUpdate(PointConfig pointConfig)
    {
        var result =  _mongoCollection.ReplaceOne(p => pointConfig.Id == p.Id, pointConfig, options: new ReplaceOptions() { IsUpsert = true });
        return result.IsAcknowledged;
    }
}