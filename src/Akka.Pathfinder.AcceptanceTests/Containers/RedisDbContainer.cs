using DotNet.Testcontainers.Builders;
using Serilog;
using StackExchange.Redis;
using IContainer = DotNet.Testcontainers.Containers.IContainer;

namespace Akka.Pathfinder.AcceptanceTests.Containers;

public class RedisContainer : IAsyncLifetime
{
    private const int InternalPort = 6379;
    private const string RedisPassword = "redispassword123";

    public RedisContainer()
    {
        Log.Information("[TEST][{RedisContainerName}] ctor", GetType().Name);

        Container = new ContainerBuilder()
            .WithImage("redis:7-alpine")
            .WithAutoRemove(true)
            .WithPortBinding(InternalPort, true)
            .WithCommand("redis-server", "--requirepass", RedisPassword)
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilMessageIsLogged("Ready to accept connections"))
            .Build();
    }

    public int Port { get; private set; }

    public string Hostname { get; private set; } = string.Empty;

    public IContainer Container { get; init; }

    public string GetConnectionString()
    {
        return $"{Hostname}:{Port},password={RedisPassword},abortConnect=false";
    }

    public async Task FlushDataAsync()
    {
        Log.Information("[TEST][{RedisContainerName}] FlushDataAsync", GetType().Name);

        var redis = await CreateRedisConnectionAsync();
        var database = redis.GetDatabase();
        
        var endpoints = redis.GetEndPoints();
        foreach (var endpoint in endpoints)
        {
            var server = redis.GetServer(endpoint);
            await server.FlushAllDatabasesAsync();
        }

        Log.Information("[TEST][{RedisContainerName}] FlushDataAsync finished", GetType().Name);
    }

    public async Task InitializeAsync()
    {
        Log.Information("[TEST][{RedisContainerName}] InitializeAsync", GetType().Name);

        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        await Container.StartAsync(timeoutCts.Token).ConfigureAwait(false);

        Port = Container.GetMappedPublicPort(InternalPort);
        Hostname = Container.Hostname;

        await VerifyConnectionAsync().ConfigureAwait(false);

        Log.Information("[TEST][{RedisContainerName}] started and ready on [{Hostname}:{PublicPort}]",
            GetType().Name, Hostname, Port);

        Log.Information("[TEST][{RedisContainerName}] InitializeAsync finished", GetType().Name);
    }

    private async Task VerifyConnectionAsync()
    {
        Log.Information("[TEST][{RedisContainerName}] Verifying Redis connection", GetType().Name);

        for (var i = 0; i < 30; i++)
        {
            try
            {
                var redis = await CreateRedisConnectionAsync();
                var database = redis.GetDatabase();
                await database.PingAsync();
                
                Log.Information("[TEST][{RedisContainerName}] Redis connection verified", GetType().Name);
                await redis.DisposeAsync();
                return;
            }
            catch (Exception ex)
            {
                if (i % 5 == 0)
                {
                    Log.Warning("[TEST][{RedisContainerName}] Attempt {Attempt} to connect failed: {Error}",
                        GetType().Name, i + 1, ex.Message);
                }

                await Task.Delay(1000).ConfigureAwait(false);
            }
        }

        throw new Exception("Failed to connect to Redis after multiple attempts");
    }

    public async Task DisposeAsync()
    {
        Log.Information("[TEST][{RedisContainerName}] DisposeAsync", GetType().Name);
        await Container.StopAsync();
        await Container.DisposeAsync();
        Log.Information("[TEST][{RedisContainerName}] DisposeAsync finished", GetType().Name);
    }

    public async Task<IConnectionMultiplexer> CreateRedisConnectionAsync()
    {
        var connectionString = GetConnectionString();
        Log.Debug("[TEST][{RedisContainerName}] Redis ConnectionString: {connectionString}", 
            GetType().Name, connectionString);
        return await ConnectionMultiplexer.ConnectAsync(connectionString);
    }

    public IConnectionMultiplexer CreateRedisConnection()
    {
        var connectionString = GetConnectionString();
        Log.Debug("[TEST][{RedisContainerName}] Redis ConnectionString: {connectionString}", 
            GetType().Name, connectionString);
        return ConnectionMultiplexer.Connect(connectionString);
    }

    public async Task<IDatabase> GetDatabaseAsync()
    {
        var connection = await CreateRedisConnectionAsync();
        return connection.GetDatabase();
    }
}