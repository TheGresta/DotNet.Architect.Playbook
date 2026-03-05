using Playbook.Persistence.Redis.Caching.Serialization;
using Playbook.Persistence.Redis.Interfaces;
using Playbook.Persistence.Redis.Models;
using Polly;
using Polly.CircuitBreaker;
using StackExchange.Redis;

namespace Playbook.Persistence.Redis.Caching;

public static class RedisCacheServiceCollectionExtensions
{
    public static IServiceCollection AddRedisCache(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind configuration
        var redisOptions = configuration.GetSection(RedisOptions.SectionName).Get<RedisOptions>()
                           ?? new RedisOptions();
        services.AddSingleton(redisOptions);

        // Register IConnectionMultiplexer as a singleton
        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var options = ConfigurationOptions.Parse(redisOptions.ConnectionString);
            options.AbortOnConnectFail = redisOptions.AbortOnConnectFail;
            options.SyncTimeout = redisOptions.SyncTimeout;
            // You can also set other options like ReconnectRetryPolicy etc.
            return ConnectionMultiplexer.Connect(options);
        });

        // Register IMemoryCache (in-memory L1 cache)
        services.AddMemoryCache();

        // Register the serializer as singleton
        services.AddSingleton<ICacheSerializer, CompositeCacheSerializer>();

        // Register resilience pipeline with Polly
        services.AddResiliencePipeline("redis-strategy", pipelineBuilder =>
        {
            pipelineBuilder
                .AddTimeout(TimeSpan.FromMilliseconds(500))
                .AddCircuitBreaker(new CircuitBreakerStrategyOptions
                {
                    FailureRatio = 0.5,
                    SamplingDuration = TimeSpan.FromSeconds(30),
                    MinimumThroughput = 5,
                    BreakDuration = TimeSpan.FromSeconds(15)
                });
        });

        // Register the HybridCacheService as the ICacheService implementation
        // Note: HybridCacheService is thread-safe and can be registered as singleton.
        // However, it depends on IMemoryCache (singleton) and IConnectionMultiplexer (singleton), so singleton is fine.
        services.AddSingleton<ICacheService, HybridCacheService>();

        return services;
    }
}
