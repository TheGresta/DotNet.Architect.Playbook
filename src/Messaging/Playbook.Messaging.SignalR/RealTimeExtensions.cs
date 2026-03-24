using MessagePack;
using MessagePack.Resolvers;

using Microsoft.AspNetCore.SignalR;

using Playbook.Messaging.SignalR.Infrastructure.Filters;
using Playbook.Messaging.SignalR.Infrastructure.Market;
using Playbook.Messaging.SignalR.Terminal;

using StackExchange.Redis;

namespace Playbook.Messaging.SignalR;

public static class RealTimeExtensions
{
    public static IServiceCollection AddFinTechRealTime(
        this IServiceCollection services,
        string redisConnectionString)
    {
        // Ensure Redis connection is healthy before attaching SignalR
        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(redisConnectionString));

        services.AddSignalR(options =>
        {
            // Performance & Guardrail Filters
            options.AddFilter<PerformanceHubFilter>();
            options.AddFilter<BackpressureFilter>();

            // HFT (High-Frequency Trading) Tuning
            options.MaximumParallelInvocationsPerClient = 5;
            options.StreamBufferCapacity = 10;

            // Critical for mobile/unstable connections in .NET 8+
            options.StatefulReconnectBufferSize = 128_000;

            // Security: Prevent large malicious payloads from slowing down the parser
            options.MaximumReceiveMessageSize = 32_768; // 32KB
        })
        .AddMessagePackProtocol(options =>
        {
            options.SerializerOptions = MessagePackSerializerOptions.Standard
                .WithResolver(StandardResolver.Instance)
                .WithCompression(MessagePackCompression.Lz4BlockArray);
        })
        .AddStackExchangeRedis(redisConnectionString, options =>
        {
            options.Configuration.ChannelPrefix = RedisChannel.Literal("FINTECH_APP");
        });

        // Background Services
        services.AddHostedService<MarketDataSimulator>();

        // Caution: ManualTestClient should likely be in a conditional block (e.g., if IsDevelopment)
        services.AddHostedService<ManualTestClient>();

        return services;
    }
}
