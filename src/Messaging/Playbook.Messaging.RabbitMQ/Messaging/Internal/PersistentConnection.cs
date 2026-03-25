using System.Collections.Concurrent;

using Playbook.Messaging.RabbitMQ.Messaging.Configuration;

using RabbitMQ.Client;

namespace Playbook.Messaging.RabbitMQ.Messaging.Internal;

internal sealed class PersistentConnection(
    IConnectionFactory connectionFactory,
    RabbitOptions options,
    ILogger<PersistentConnection> logger) : IAsyncDisposable
{
    private IConnection? _connection;
    private bool _disposed;
    private readonly ConcurrentQueue<IChannel> _channelPool = new();
    private readonly SemaphoreSlim _poolLock = new(options.ChannelPoolSize, options.ChannelPoolSize);
    private readonly SemaphoreSlim _connectionLock = new(1, 1);

    public bool IsConnected => _connection is { IsOpen: true } && !_disposed;

    public async ValueTask<IChannel> GetChannelAsync(CancellationToken ct = default)
    {
        if (!IsConnected) await TryConnectAsync(ct);

        await _poolLock.WaitAsync(ct);

        try
        {
            if (_channelPool.TryDequeue(out var channel) && channel.IsOpen)
            {
                return channel;
            }

            // Create a new channel if pool is empty or channel was dead
            return await _connection!.CreateChannelAsync(cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _poolLock.Release(); // Must release if creation fails
            logger.LogError(ex, "Failed to create or retrieve RabbitMQ channel.");
            throw;
        }
    }

    public void ReturnChannel(IChannel channel)
    {
        if (channel.IsOpen && !_disposed)
        {
            _channelPool.Enqueue(channel);
            _poolLock.Release();
        }
        else
        {
            channel.Dispose();
            _poolLock.Release();
        }
    }

    public async ValueTask<bool> TryConnectAsync(CancellationToken ct = default)
    {
        if (IsConnected) return true;

        await _connectionLock.WaitAsync(ct);
        try
        {
            if (IsConnected) return true;

            // Clear invalid channels from a previous connection
            while (_channelPool.TryDequeue(out var oldChannel))
                oldChannel.Dispose();

            _connection = await connectionFactory.CreateConnectionAsync(ct);

            _connection.ConnectionShutdownAsync += async (s, e) =>
            {
                logger.LogWarning("RabbitMQ connection lost. Reason: {Reason}", e.ReplyText);
                // We don't reconnect here immediately to avoid infinite loops during network outages.
                // The next 'GetChannelAsync' call will trigger the reconnect.
            };

            return true;
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        while (_channelPool.TryDequeue(out var channel))
            channel.Dispose();

        if (_connection != null) await _connection.CloseAsync();
    }
}
