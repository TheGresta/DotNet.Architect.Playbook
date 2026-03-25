using MessagePack;
using MessagePack.Resolvers;

using Microsoft.AspNetCore.SignalR;

using Playbook.Messaging.SignalR.Infrastructure.Filters;
using Playbook.Messaging.SignalR.Infrastructure.Market;
using Playbook.Messaging.SignalR.Terminal;

using StackExchange.Redis;

namespace Playbook.Messaging.SignalR;

/// <summary>
/// Provides centralized service registration and configuration for the high-performance FinTech real-time engine.
/// This class follows the .NET "Add{Feature}" pattern to encapsulate complex SignalR and Redis setup.
/// </summary>
public static class RealTimeExtensions
{
    /// <summary>
    /// Configures the application's real-time messaging pipeline, including SignalR Hubs, 
    /// Redis backplane integration, and performance monitoring filters.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="redisConnectionString">The connection string for the Redis cluster used for horizontal scaling.</param>
    /// <returns>The modified <see cref="IServiceCollection"/> for method chaining.</returns>
    /// <remarks>
    /// This extension method applies specific "HFT-style" (High-Frequency Trading) tunings, such as 
    /// MessagePack LZ4 compression, stateful reconnection buffers, and parallel invocation limits 
    /// to ensure system stability under extreme load.
    /// </remarks>
    public static IServiceCollection AddFinTechRealTime(
        this IServiceCollection services,
        string redisConnectionString)
    {
        // Infrastructure: Establishing a singleton connection to Redis. 
        // Shared across SignalR and potentially other application caching layers to conserve sockets.
        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(redisConnectionString));

        services.AddSignalR(options =>
        {
            // Middleware: Performance & Guardrail Filters
            // Automatically profiles hub methods and monitors transport health for every invocation.
            options.AddFilter<PerformanceHubFilter>();
            options.AddFilter<BackpressureFilter>();

            // HFT Tuning: Limits concurrent requests per connection to prevent head-of-line blocking 
            // and manages the internal stream buffer to favor low-latency over high-throughput queuing.
            options.MaximumParallelInvocationsPerClient = 5;
            options.StreamBufferCapacity = 10;

            // Resilience: Enables Stateful Reconnect (introduced in .NET 8) with a 128KB buffer, 
            // allowing clients to resume their session without losing messages during transient drops.
            options.StatefulReconnectBufferSize = 128_000;

            // Security: Hard limit on inbound payload size to mitigate Large Object Heap (LOH) fragmentation 
            // and Denial of Service (DoS) attacks via oversized messages.
            options.MaximumReceiveMessageSize = 32_768; // 32KB
        })
        // Serialization: MessagePack is significantly faster and more compact than JSON.
        // Lz4BlockArray compression is utilized to minimize bandwidth for repetitive market data structures.
        .AddMessagePackProtocol(options =>
        {
            options.SerializerOptions = MessagePackSerializerOptions.Standard
                .WithResolver(StandardResolver.Instance)
                .WithCompression(MessagePackCompression.Lz4BlockArray);
        })
        // Scaling: The StackExchange Redis backplane ensures that price updates published on 
        // Instance A are seamlessly broadcast to clients connected to Instance B through Z.
        .AddStackExchangeRedis(redisConnectionString, options =>
        {
            options.Configuration.ChannelPrefix = RedisChannel.Literal("FINTECH_APP");
        });

        // Background Services: The "Engine Room" of the application.
        // Generates the synthetic market data consumed by the SignalR Hub.
        services.AddHostedService<MarketDataSimulator>();

        // Environment-specific logic: In a production scenario, this would typically be wrapped 
        // in an environment check (e.g., if (env.IsDevelopment())) to avoid running test clients in prod.
        services.AddHostedService<ManualTestClient>();

        return services;
    }
}
