using Microsoft.Extensions.Options;
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
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // 1. Robust Configuration Binding with Validation
        services.AddOptions<RedisOptions>()
            .BindConfiguration(RedisOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // 2. Optimized Redis Connection (Lazy & Non-Blocking)
        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var redisOptions = sp.GetRequiredService<IOptions<RedisOptions>>().Value;

            // FIX: Parse the full string first to handle password, ssl, etc.
            var configurationOptions = ConfigurationOptions.Parse(redisOptions.ConnectionString);

            // Apply overrides from your RedisOptions class
            configurationOptions.AbortOnConnectFail = redisOptions.AbortOnConnectFail;
            configurationOptions.SyncTimeout = redisOptions.SyncTimeout;
            configurationOptions.ConnectTimeout = 5000;
            configurationOptions.ReconnectRetryPolicy = new ExponentialRetry(5000);

            // Architect's Note: Connect is synchronous but execution happens once during Singleton resolution.
            return ConnectionMultiplexer.Connect(configurationOptions);
        });

        // 3. Infrastructure Layers
        services.AddMemoryCache();
        services.AddSingleton<ICacheSerializer, CompositeCacheSerializer>();

        // 4. Modern Polly v8 Resilience Pipeline
        services.AddResiliencePipeline("redis-strategy", (builder, context) =>
        {
            builder
                .AddTimeout(TimeSpan.FromMilliseconds(500))
                .AddCircuitBreaker(new CircuitBreakerStrategyOptions
                {
                    FailureRatio = 0.5,
                    SamplingDuration = TimeSpan.FromSeconds(30),
                    MinimumThroughput = 5,
                    BreakDuration = TimeSpan.FromSeconds(15),
                    ShouldHandle = new PredicateBuilder().Handle<RedisException>().Handle<TimeoutException>()
                });
        });

        // 5. Core Service Registration
        services.AddSingleton<ICacheService, HybridCacheService>();

        return services;
    }
}
