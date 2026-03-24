using MessagePack;

using Microsoft.AspNetCore.SignalR;

using Playbook.Messaging.SignalR.Infrastructure.Filters;
using Playbook.Messaging.SignalR.Infrastructure.Market;
using Playbook.Messaging.SignalR.Terminal;

namespace Playbook.Messaging.SignalR;

public static class RealTimeExtensions
{
    public static void AddFinTechRealTime(this IServiceCollection services, string redisConnectionString)
    {
        services.AddSignalR(options =>
        {
            options.AddFilter<PerformanceHubFilter>();
            options.AddFilter<BackpressureFilter>();
            // Maximum amount of data the server will buffer for a single client
            options.MaximumParallelInvocationsPerClient = 5;
            options.StreamBufferCapacity = 10;
            options.StatefulReconnectBufferSize = 100_000;
        })
        .AddMessagePackProtocol(options =>
        {
            options.SerializerOptions = MessagePackSerializerOptions.Standard
                .WithResolver(MessagePack.Resolvers.StandardResolver.Instance)
                // LZ4 provides the best balance for real-time streaming
                .WithCompression(MessagePackCompression.Lz4BlockArray);
        })
        .AddStackExchangeRedis(redisConnectionString);

        // Register our High-Frequency Market Simulator
        services.AddHostedService<MarketDataSimulator>();

        services.AddHostedService<ManualTestClient>();
    }
}
