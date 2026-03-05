using Microsoft.Extensions.Options;
using Playbook.Persistence.Redis.Caching.Serialization;
using Playbook.Persistence.Redis.Interfaces;
using Playbook.Persistence.Redis.Models;
using Polly;
using Polly.CircuitBreaker;
using StackExchange.Redis;

namespace Playbook.Persistence.Redis.Caching;

/// <summary>
/// Provides extension methods for <see cref="IServiceCollection"/> to configure and register 
/// Redis-based hybrid caching infrastructure.
/// </summary>
public static class RedisCacheServiceCollectionExtensions
{
    /// <summary>
    /// Adds a resilient hybrid caching system to the service collection, integrating L1 Memory Cache 
    /// and L2 Redis Cache with a Polly-based resilience strategy.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="configuration">The <see cref="IConfiguration"/> instance to bind <see cref="RedisOptions"/> from.</param>
    /// <returns>The same <see cref="IServiceCollection"/> instance so that multiple calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> or <paramref name="configuration"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// This method performs the following registrations:
    /// <list type="number">
    /// <item><description>Configures <see cref="RedisOptions"/> with data annotation validation and start-up verification.</description></item>
    /// <item><description>Registers <see cref="IConnectionMultiplexer"/> as a singleton with optimized reconnection policies.</description></item>
    /// <item><description>Configures a keyed Polly v8 resilience pipeline named <c>"redis-strategy"</c> featuring a circuit breaker and timeout.</description></item>
    /// <item><description>Registers <see cref="ICacheSerializer"/> and <see cref="ICacheService"/> implementations.</description></item>
    /// </list>
    /// </remarks>
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

            var configurationOptions = ConfigurationOptions.Parse(redisOptions.ConnectionString);

            configurationOptions.AbortOnConnectFail = redisOptions.AbortOnConnectFail;
            configurationOptions.SyncTimeout = redisOptions.SyncTimeout;
            configurationOptions.ConnectTimeout = 5000;
            configurationOptions.ReconnectRetryPolicy = new ExponentialRetry(5000);

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
