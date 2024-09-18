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
    private const string AESPrefix = "AES";
    private const string NoEncryptionPrefix = "NO-";
    private const string Key = "userCloudId_c582fb1e-a171-4994-b7e2-fbefc25fc95c_tenantId_c582fb1e-a171-4994-b7e2-fbefc25fc95c_orgId_c582fb1e-a171-4994-b7e2-fbefc25fc95c_cloudIdUserInfo";
    private string AESKeyWithPrefix => $"{AESPrefix}{Key}";
    private string NoEncryptionKeyWithPrefix => $"{NoEncryptionPrefix}{Key}";
    private const string Value = "{\"UserId\":\"123e4567-e89b-12d3-a456-426614174000\",\"Email\":\"john.doe@example.com\",\"FullName\":\"John Doe\",\"IsDeveloper\":true,\"IsBlockedOrExpired\":false,\"TenantId\":\"123e4567-e89b-12d3-a456-426614174001\",\"OrganizationId\":\"123e4567-e89b-12d3-a456-426614174002\",\"LegislationCode\":\"US\",\"TimeZoneId\":\"America/New_York\",\"InitialOrganizationId\":\"123e4567-e89b-12d3-a456-426614174003\",\"FeatureCodes\":[\"FeatureA\",\"FeatureB\"],\"BusinessId\":\"123e4567-e89b-12d3-a456-426614174004\"}";
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
        await _redisConnectionMultiplexerService.SetKeyValueAsync(AESKeyWithPrefix, Value, useEncryption: true);
        await _redisConnectionMultiplexerService.GetKeyValueAsync<string>(AESKeyWithPrefix, useEncryption: true);
    }

    [Benchmark]
    public async Task TestRedisWithoutEncryption()
    {
        var keyWithPrefix = $"{NoEncryptionPrefix}{Key}";
        await _redisConnectionMultiplexerService.SetKeyValueAsync(NoEncryptionKeyWithPrefix, Value, useEncryption: false);
        await _redisConnectionMultiplexerService.GetKeyValueAsync<string>(NoEncryptionKeyWithPrefix, useEncryption: false);
    }

    [Benchmark]
    public async Task TestRedisEncryptionFor1000Keys()
    {
        for (int i = 0; i < 1000; i++)
        {
            await _redisConnectionMultiplexerService.SetKeyValueAsync($"{AESKeyWithPrefix}{i}", Value, useEncryption: true);
            await _redisConnectionMultiplexerService.GetKeyValueAsync<string>($"{AESKeyWithPrefix}{i}", useEncryption: true);
        }
    }

    [Benchmark]
    public async Task TestRedisWithoutEncryptionFor1000Keys()
    {
        for (int i = 0; i < 1000; i++)
        {
            await _redisConnectionMultiplexerService.SetKeyValueAsync($"{NoEncryptionKeyWithPrefix}{i}", Value, useEncryption: false);
            await _redisConnectionMultiplexerService.GetKeyValueAsync<string>($"{NoEncryptionKeyWithPrefix}{i}", useEncryption: false);
        }
    }

    // cleanup code
    [GlobalCleanup]
    public void Cleanup()
    {
        // This method is intentionally left empty
    }
}