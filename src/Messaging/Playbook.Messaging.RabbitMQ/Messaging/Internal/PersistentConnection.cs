using System.Collections.Concurrent;

using Playbook.Messaging.RabbitMQ.Messaging.Configuration;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Playbook.Messaging.RabbitMQ.Messaging.Internal;

internal sealed class PersistentConnection(
    IConnectionFactory connectionFactory,
    RabbitOptions options,
    ILogger<PersistentConnection> logger) : IAsyncDisposable
{
    private readonly ConcurrentQueue<IChannel> _channelPool = new();
    private readonly SemaphoreSlim _poolLimit = new(options.ChannelPoolSize, options.ChannelPoolSize);
    private readonly SemaphoreSlim _connectionLock = new(1, 1);

    private IConnection? _connection;
    private bool _disposed;

    public bool IsConnected => _connection is { IsOpen: true } && !_disposed;

    /// <summary>
    /// Acquires a channel wrapped in a lease. The channel is returned to the pool automatically when disposed.
    /// </summary>
    public async ValueTask<ChannelLease> AcquireAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!IsConnected)
        {
            await TryConnectAsync(ct).ConfigureAwait(false);
        }

        await _poolLimit.WaitAsync(ct).ConfigureAwait(false);

        try
        {
            IChannel? channel = null;
            while (_channelPool.TryDequeue(out var pooledChannel))
            {
                if (pooledChannel.IsOpen)
                {
                    channel = pooledChannel;
                    break;
                }
                await DisposeChannelInternalAsync(pooledChannel).ConfigureAwait(false);
            }

            channel ??= await CreateNewChannelAsync(ct).ConfigureAwait(false);

            return new ChannelLease(this, channel);
        }
        catch (Exception ex)
        {
            _poolLimit.Release();
            logger.LogError(ex, "Failed to acquire RabbitMQ channel lease.");
            throw;
        }
    }

    private async ValueTask ReturnChannelAsync(IChannel channel)
    {
        if (_disposed || !channel.IsOpen)
        {
            await DisposeChannelInternalAsync(channel).ConfigureAwait(false);
            _poolLimit.Release();
            return;
        }

        _channelPool.Enqueue(channel);
        _poolLimit.Release();
    }

    public async ValueTask<bool> TryConnectAsync(CancellationToken ct = default)
    {
        if (IsConnected) return true;

        await _connectionLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (IsConnected) return true;

            await CleanupPoolAsync().ConfigureAwait(false);

            _connection = await connectionFactory.CreateConnectionAsync(ct).ConfigureAwait(false);
            _connection.ConnectionShutdownAsync += OnConnectionShutdown;

            logger.LogInformation("RabbitMQ Client connected to {Endpoint}", _connection.Endpoint);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Fatal error connecting to RabbitMQ node.");
            return false;
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    private Task OnConnectionShutdown(object sender, ShutdownEventArgs e)
    {
        logger.LogWarning("RabbitMQ connection lost. Reason: {Reason}", e.ReplyText);
        return Task.CompletedTask;
    }

    private async ValueTask<IChannel> CreateNewChannelAsync(CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(_connection);
        return await _connection.CreateChannelAsync(cancellationToken: ct).ConfigureAwait(false);
    }

    private async ValueTask CleanupPoolAsync()
    {
        while (_channelPool.TryDequeue(out var oldChannel))
        {
            await DisposeChannelInternalAsync(oldChannel).ConfigureAwait(false);
        }
    }

    private static async ValueTask DisposeChannelInternalAsync(IChannel channel)
    {
        try
        {
            if (channel.IsOpen) await channel.CloseAsync().ConfigureAwait(false);
            channel.Dispose();
        }
        catch { /* Suppression for background cleanup */ }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        await CleanupPoolAsync().ConfigureAwait(false);

        if (_connection is not null)
        {
            _connection.ConnectionShutdownAsync -= OnConnectionShutdown;
            await _connection.CloseAsync().ConfigureAwait(false);
            _connection.Dispose();
        }

        _poolLimit.Dispose();
        _connectionLock.Dispose();
    }

    public readonly struct ChannelLease(PersistentConnection connection, IChannel channel) : IAsyncDisposable
    {
        public IChannel Channel { get; } = channel;

        public async ValueTask DisposeAsync() =>
            // Now we can properly await the return logic
            await connection.ReturnChannelAsync(Channel).ConfigureAwait(false);
    }
}
