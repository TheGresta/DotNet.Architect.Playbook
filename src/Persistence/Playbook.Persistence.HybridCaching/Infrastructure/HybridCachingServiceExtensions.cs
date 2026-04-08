using System.Reflection;

using Microsoft.Extensions.Caching.Hybrid;

using Playbook.Persistence.HybridCaching.Core.Configuration;
using Playbook.Persistence.HybridCaching.Core.Entities;
using Playbook.Persistence.HybridCaching.Core.Interfaces;
using Playbook.Persistence.HybridCaching.Core.Providers;
using Playbook.Persistence.HybridCaching.Infrastructure.Caching;
using Playbook.Persistence.HybridCaching.Infrastructure.Providers;

using StackExchange.Redis;

namespace Playbook.Persistence.HybridCaching.Infrastructure;

/// <summary>
/// Provides extension methods for <see cref="IServiceCollection"/> to configure the high-performance 
/// hybrid caching infrastructure, including Redis L2, Brotli compression, and automated policy registration.
/// </summary>
public static class HybridCachingServiceExtensions
{
    /// <summary>
    /// Registers all necessary services for the playbook caching system, including distributed Redis, 
    /// custom serializers, and domain providers.
    /// </summary>
    /// <param name="services">The service collection to add the caching infrastructure to.</param>
    /// <param name="configuration">The application configuration containing "Redis" and "CacheSettings" sections.</param>
    /// <returns>The updated <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddPlaybookCaching(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Redis Configuration
        // Configures the StackExchange.Redis multiplexer for shared use across the L2 cache and potential Pub/Sub needs.
        var redisSection = configuration.GetSection("Redis");
        var redisOptions = new ConfigurationOptions
        {
            EndPoints = { redisSection["Host"]! },
            Password = redisSection["Password"],
            AbortOnConnectFail = false, // Prevents startup failure if Redis is temporarily unreachable.
            ConnectTimeout = 5000,
            SyncTimeout = 5000
        };

        // Singleton Multiplexer ensures a single connection pool is shared throughout the application lifecycle.
        var multiplexer = ConnectionMultiplexer.Connect(redisOptions);
        services.AddSingleton<IConnectionMultiplexer>(multiplexer);

        // Standard L2 Distributed Cache Configuration used by HybridCache as the secondary storage layer.
        services.AddStackExchangeRedisCache(options =>
        {
            options.ConfigurationOptions = redisOptions;
            options.InstanceName = "Playbook_";
        });

        // 2. Core Cache Logic & Settings
        // Binds the CacheSettings POCO to the configuration and registers providers for key management.
        services.Configure<CacheSettings>(configuration.GetSection("CacheSettings"));
        services.AddSingleton<ICacheKeyProvider, CacheKeyProvider>();
        services.AddSingleton<ICacheTagFactory, CacheTagFactory>();
        services.AddSingleton<ICacheProvider, CacheProvider>();

        // 3. HybridCache & Smart Serializers
        // Initializes the .NET 8 HybridCache (L1 + L2) with custom limits and binary-efficient serializers.
        services.AddHybridCache(options =>
        {
            // Sets a hard cap on L1 memory usage to prevent OutOfMemory exceptions during high-traffic bursts.
            options.MaximumPayloadBytes = 120 * 1024 * 1024; // 120MB

            options.DefaultEntryOptions = new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(30),
                LocalCacheExpiration = TimeSpan.FromMinutes(5)
            };
        })
        // Registers specialized serializers that handle Brotli compression and Protobuf logic for specific types.
        .AddSerializer<Product, SmartTechSerializer<Product>>()
        .AddSerializer<List<Product>, SmartTechSerializer<List<Product>>>();

        // 4. Custom Domain Providers
        // Scoped registration for data providers that consume the ICacheProvider.
        services.AddScoped<IProductProvider, ProductProvider>();
        services.AddScoped<ICasualWayProductProvider, CasualWayProductProvider>();

        // 5. Add Policies
        // Automatically scans the assembly to find and register ICachePolicy implementations.
        services.AddCachePolicies(Assembly.GetExecutingAssembly());

        return services;
    }

    /// <summary>
    /// Scans the specified assembly for all non-abstract classes implementing <see cref="ICachePolicy{T}"/> 
    /// and registers them as singletons.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assembly">The assembly to scan for policy implementations.</param>
    /// <returns>The updated <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddCachePolicies(this IServiceCollection services, Assembly assembly)
    {
        // 1. Reflection scan: Find all non-abstract classes that implement the generic ICachePolicy interface.
        var policyTypes = assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .Select(t => new
            {
                Implementation = t,
                // Identifies the closed-generic interface (e.g., ICachePolicy<Product>) for DI registration.
                Interface = t.GetInterfaces()
                    .FirstOrDefault(i => i.IsGenericType &&
                                         i.GetGenericTypeDefinition() == typeof(ICachePolicy<>))
            })
            .Where(t => t.Interface != null);

        foreach (var policy in policyTypes)
        {
            // 2. Register as Singleton: Since policies are stateless configuration rules, 
            // a Singleton lifetime is optimal for performance and memory.
            services.AddSingleton(policy.Interface!, policy.Implementation);
        }

        return services;
    }
}