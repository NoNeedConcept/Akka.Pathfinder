using MongoDB.Driver;
using Path = Akka.Pathfinder.Core.Persistence.Data.Path;

namespace Akka.Pathfinder.Core.Services;

public interface IPathWriter : IPathReader
{
    public bool AddOrUpdate(Path path);
}

public class PathWriter : PathReader, IPathWriter
{
    private IMongoCollection<Path> _mongoCollection { get; set; }

    public PathWriter(IMongoCollection<Path> collection) : base(collection)
    {
        _mongoCollection = collection;
    }

    public bool AddOrUpdate(Path path)
    {
        var result =  _mongoCollection.ReplaceOne(p => path.Id == p.Id, path, options: new ReplaceOptions() { IsUpsert = true });
        return result.IsAcknowledged;
    }
}
