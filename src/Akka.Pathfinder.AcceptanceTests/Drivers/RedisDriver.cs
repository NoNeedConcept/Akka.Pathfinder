using Akka.Pathfinder.AcceptanceTests.Containers;
using Reqnroll.BoDi;

namespace Akka.Pathfinder.AcceptanceTests.Drivers;

public class RedisDriver
{
    private readonly RedisContainer _redisContainer;

    public RedisDriver(IObjectContainer container)
    {
        _redisContainer = container.Resolve<RedisContainer>();
    }
    
    public async Task<List<string>> GetAllKeysAsync(string pattern = "*")
    {
        await using var connection = await _redisContainer.CreateRedisConnectionAsync();
        var endpoints = connection.GetEndPoints();
        var server = connection.GetServer(endpoints[0]);
        return server.Keys(pattern: pattern).Select(k => k.ToString()).ToList();
    }
}
