using Akka.Pathfinder.Core.Configs;
using MongoDB.Driver;

namespace Akka.Pathfinder.Core.Services;

public interface IPointConfigReader
{
    IQueryable<PointConfig> Get();

    IQueryable<PointConfig> Get(int pointId);
}

public class PointConfigReader : IPointConfigReader
{
    private readonly IMongoCollection<PointConfig> _mongoCollection;
    public PointConfigReader(IMongoCollection<PointConfig> collection)
    {
        _mongoCollection = collection;
    }

    public IQueryable<PointConfig> Get() => _mongoCollection.AsQueryable();

    public IQueryable<PointConfig> Get(int pointId) => Get().Where(x => x.Id == pointId);
}
