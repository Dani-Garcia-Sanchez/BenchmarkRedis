using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Microsoft.Extensions.Options;
using Moq;
using Sage.ES.S200.Infrastructure.Cache.CacheSerializer;
using Sage.Eureka.Common.Cache.Abstractions.ConfigurationModel;
using Sage.Eureka.Common.Cache.Redis;
using Sage.Eureka.Common.Tools.Activities;
using StackExchange.Redis;

internal class Program
{
    private static void Main(string[] args)
    {
        BenchmarkRunner.Run<RedisEncryptionBenchmark>();
    }
}

[AllStatisticsColumn]
[MemoryDiagnoser]
public class RedisEncryptionBenchmark
{
    RedisConnectionMultiplexerService _redisConnectionMultiplexerService;

    [GlobalSetup]
    public void Setup()
    {
        var cacheSettings = new CacheSettings { ConnectionString = "127.0.0.1:6379", SymmetricEncryptionKey = "VHdvIE9uZSBOaW5lIFR3bw==", KeyPrefix = "benchmark" };
        var optionsMonitor = new Mock<IOptionsMonitor<CacheSettings>>();
        optionsMonitor.Setup(o => o.CurrentValue).Returns(cacheSettings);
        _redisConnectionMultiplexerService = new RedisConnectionMultiplexerService(
            ConnectionMultiplexer.Connect("localhost:6379"),
            optionsMonitor.Object,
            new ProtobufNetCacheSerializer(),
            new Mock<IActivitySourceConfiguration>().Object
            );
    }

    [Benchmark]
    public async Task TestRedisEncryption()
    {
        await _redisConnectionMultiplexerService.SetKeyValueAsync("benchmark-AES", "benchmark", useEncryption: true);
        await _redisConnectionMultiplexerService.GetKeyValueAsync<string>("benchmark-AES", useEncryption: true);
    }

    [Benchmark]
    public async Task TestRedisWithoutEncryption()
    {
        await _redisConnectionMultiplexerService.SetKeyValueAsync("benchmark-noencryption", "benchmark", useEncryption: false);
        await _redisConnectionMultiplexerService.GetKeyValueAsync<string>("benchmark-noencryption", useEncryption: false);
    }

    // cleanup code
    [GlobalCleanup]
    public void Cleanup()
    {
        // This method is intentionally left empty
    }
}