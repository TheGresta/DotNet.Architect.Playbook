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

public static class HybridCachingServiceExtensions
{
    public static IServiceCollection AddPlaybookCaching(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Redis Configuration
        var redisSection = configuration.GetSection("Redis");
        var redisOptions = new ConfigurationOptions
        {
            EndPoints = { redisSection["Host"]! },
            Password = redisSection["Password"],
            AbortOnConnectFail = false,
            ConnectTimeout = 5000,
            SyncTimeout = 5000
        };

        // Singleton Multiplexer for Pub/Sub and direct Redis commands
        var multiplexer = ConnectionMultiplexer.Connect(redisOptions);
        services.AddSingleton<IConnectionMultiplexer>(multiplexer);

        // L2 Distributed Cache Configuration
        services.AddStackExchangeRedisCache(options =>
        {
            options.ConfigurationOptions = redisOptions;
            options.InstanceName = "Playbook_";
        });

        // 2. Core Cache Logic & Settings
        services.Configure<CacheSettings>(configuration.GetSection("CacheSettings"));
        services.AddSingleton<ICacheKeyProvider, CacheKeyProvider>();
        services.AddSingleton<ICacheProvider, CacheProvider>();

        // 3. HybridCache & Smart Serializers
        services.AddHybridCache(options =>
        {
            // Total size of the L1 (Memory) cache across all items
            options.MaximumPayloadBytes = 120 * 1024 * 1024; // 120MB

            options.DefaultEntryOptions = new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(30),
                LocalCacheExpiration = TimeSpan.FromMinutes(5)
            };
        })
        .AddSerializer<Product, SmartTechSerializer<Product>>()
        .AddSerializer<List<Product>, SmartTechSerializer<List<Product>>>();

        // 4. Custom Domain Providers
        services.AddScoped<IProductProvider, ProductProvider>();
        services.AddScoped<ICasualWayProductProvider, CasualWayProductProvider>();

        // 5. Add Policies
        services.AddCachePolicies(Assembly.GetExecutingAssembly());

        return services;
    }

    public static IServiceCollection AddCachePolicies(this IServiceCollection services, Assembly assembly)
    {
        // 1. Find all non-abstract classes that implement ICachePolicy<T>
        var policyTypes = assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .Select(t => new
            {
                Implementation = t,
                // Find the specific ICachePolicy<T> interface it implements
                Interface = t.GetInterfaces()
                    .FirstOrDefault(i => i.IsGenericType &&
                                         i.GetGenericTypeDefinition() == typeof(ICachePolicy<>))
            })
            .Where(t => t.Interface != null);

        foreach (var policy in policyTypes)
        {
            // 2. Register as Singleton (Policies are stateless rules, so Singleton is best)
            services.AddSingleton(policy.Interface!, policy.Implementation);
        }

        return services;
    }
}
