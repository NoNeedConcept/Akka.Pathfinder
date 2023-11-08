using Akka.Pathfinder.Core.Configs;
using MongoDB.Driver;

namespace Akka.Pathfinder.Core.Services;

public interface IPointConfigWriter : IPointConfigReader
{
    public void AddPointConfigs(Guid CollectionId, List<PointConfig> pointConfigs);
    public void AddPointConfig(Guid CollectionId, PointConfig pointConfig);
}
public class PointConfigWriter : PointConfigReader, IPointConfigWriter
{

    public PointConfigWriter(IMongoDatabase database) : base(database)
    { }

    public void AddPointConfigs(Guid CollectionId, List<PointConfig> pointConfigs)
        => Database
        .GetCollection<PointConfig>(CollectionId.ToString())
        .InsertMany(pointConfigs, new InsertManyOptions() { IsOrdered = true });

    public void AddPointConfig(Guid CollectionId, PointConfig pointConfig)
        => Database
        .GetCollection<PointConfig>(CollectionId.ToString())
        .InsertOne(pointConfig);
}