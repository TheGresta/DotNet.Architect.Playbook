using System.Collections.Concurrent;

using Playbook.Messaging.RabbitMQ.Messaging.Configuration;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Playbook.Messaging.RabbitMQ.Messaging.Internal;

/// <summary>
/// Represents a high-performance, resilient wrapper over a RabbitMQ <see cref="IConnection"/>.
/// Implements a thread-safe channel pooling mechanism to optimize resource utilization and reduce overhead 
/// associated with frequent channel creation and disposal.
/// </summary>
/// <remarks>
/// This implementation utilizes <see cref="SemaphoreSlim"/> for asynchronous throttling and 
/// <see cref="ConcurrentQueue{T}"/> for non-blocking pool management, ensuring optimal throughput 
/// in high-concurrency .NET 10 environments.
/// </remarks>
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

    /// <summary>
    /// Gets a value indicating whether the underlying connection is established and active.
    /// </summary>
    public bool IsConnected => _connection is { IsOpen: true } && !_disposed;

    /// <summary>
    /// Acquires a <see cref="IChannel"/> wrapped in a <see cref="ChannelLease"/>. 
    /// If a valid channel is available in the pool, it is reused; otherwise, a new channel is instantiated.
    /// </summary>
    /// <param name="ct">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>A <see cref="ValueTask{ChannelLease}"/> that returns the leased channel upon completion.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the connection has been disposed.</exception>
    /// <exception cref="Exception">Thrown if channel acquisition fails after exhausting pool logic.</exception>
    public async ValueTask<ChannelLease> AcquireAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!IsConnected)
        {
            await TryConnectAsync(ct).ConfigureAwait(false);
        }

        // Asynchronously wait for an available slot in the channel pool to prevent resource exhaustion
        await _poolLimit.WaitAsync(ct).ConfigureAwait(false);

        try
        {
            IChannel? channel = null;
            // Drain the pool of closed or invalid channels before creating a new one
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

    /// <summary>
    /// Returns a channel to the internal pool or disposes of it if the connection is closed or the channel is faulted.
    /// </summary>
    /// <param name="channel">The <see cref="IChannel"/> to be returned or disposed.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
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

    /// <summary>
    /// Attempts to establish a connection to the RabbitMQ broker if not already connected.
    /// Uses double-check locking to ensure thread safety during connection negotiation.
    /// </summary>
    /// <param name="ct">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>A <see cref="ValueTask{Boolean}"/> indicating whether the connection is successful.</returns>
    public async ValueTask<bool> TryConnectAsync(CancellationToken ct = default)
    {
        if (IsConnected) return true;

        await _connectionLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            // Re-verify connection state after acquiring lock to prevent redundant connection attempts
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

    /// <summary>
    /// Handles the <see cref="IConnection.ConnectionShutdownAsync"/> event to log connection loss.
    /// </summary>
    private Task OnConnectionShutdown(object sender, ShutdownEventArgs e)
    {
        logger.LogWarning("RabbitMQ connection lost. Reason: {Reason}", e.ReplyText);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Factory method for creating a new <see cref="IChannel"/> instance.
    /// </summary>
    private async ValueTask<IChannel> CreateNewChannelAsync(CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(_connection);
        return await _connection.CreateChannelAsync(cancellationToken: ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Purges all channels currently residing in the pool and ensures they are properly closed and disposed.
    /// </summary>
    private async ValueTask CleanupPoolAsync()
    {
        while (_channelPool.TryDequeue(out var oldChannel))
        {
            await DisposeChannelInternalAsync(oldChannel).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Internal helper to safely close and dispose of a channel, suppressing exceptions to avoid interrupting cleanup flows.
    /// </summary>
    private static async ValueTask DisposeChannelInternalAsync(IChannel channel)
    {
        try
        {
            if (channel.IsOpen) await channel.CloseAsync().ConfigureAwait(false);
            channel.Dispose();
        }
        catch { /* Suppression for background cleanup */ }
    }

    /// <summary>
    /// Performs asynchronous tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    /// <returns>A <see cref="ValueTask"/> representing the disposal process.</returns>
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

    /// <summary>
    /// A lightweight, disposable wrapper for an <see cref="IChannel"/>. 
    /// Ensures that the channel is returned to the <see cref="PersistentConnection"/> pool when the lease is disposed.
    /// </summary>
    /// <param name="connection">The parent persistent connection managing the pool.</param>
    /// <param name="channel">The acquired RabbitMQ channel.</param>
    public readonly struct ChannelLease(PersistentConnection connection, IChannel channel) : IAsyncDisposable
    {
        /// <summary>
        /// Gets the RabbitMQ channel associated with this lease.
        /// </summary>
        public IChannel Channel { get; } = channel;

        /// <summary>
        /// Returns the channel to the pool asynchronously.
        /// </summary>
        public async ValueTask DisposeAsync() =>
            await connection.ReturnChannelAsync(Channel).ConfigureAwait(false);
    }
}
