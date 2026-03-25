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

    // Tracks the number of ChannelLease instances currently held by callers.
    // DisposeAsync waits for this to reach zero before disposing semaphores,
    // preventing ObjectDisposedException from concurrent ReturnChannelAsync calls.
    private int _activeLeases;

    /// <summary>
    /// Gets a value indicating whether the underlying connection is established and active.
    /// </summary>
    public bool IsConnected => _connection is { IsOpen: true } && !_disposed;

    /// <summary>
    /// Acquires a pooled channel lease for standard (fire-and-forget) publishing or consuming.
    /// If a valid channel is available in the pool, it is reused; otherwise, a new channel is instantiated.
    /// </summary>
    /// <param name="ct">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>A <see cref="ValueTask{ChannelLease}"/> that returns the leased channel upon completion.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the connection has been disposed.</exception>
    /// <exception cref="Exception">Thrown if channel acquisition fails after exhausting pool logic.</exception>
    public async ValueTask<ChannelLease> AcquireAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!IsConnected && !await TryConnectAsync(ct).ConfigureAwait(false))
            throw new InvalidOperationException("Failed to establish RabbitMQ connection.");

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

            channel ??= await CreateNewChannelAsync(options: null, ct).ConfigureAwait(false);

            // Register the lease BEFORE handing it to the caller so DisposeAsync
            // can safely wait for the count to reach zero.
            Interlocked.Increment(ref _activeLeases);
            return new ChannelLease(this, channel, returnToPool: true);
        }
        catch (Exception ex)
        {
            _poolLimit.Release();
            logger.LogError(ex, "Failed to acquire RabbitMQ channel lease.");
            throw;
        }
    }

    /// <summary>
    /// Acquires a dedicated, non-pooled channel with publisher confirmations enabled.
    /// The channel is disposed (not returned to the pool) when the lease is released.
    /// Use this when <see cref="MessageEndpointDefinition.WaitForConfirm"/> is <c>true</c>.
    /// </summary>
    public async ValueTask<ChannelLease> AcquireConfirmChannelAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!IsConnected && !await TryConnectAsync(ct).ConfigureAwait(false))
            throw new InvalidOperationException("Failed to establish RabbitMQ connection.");

        // Throttle confirm channels with the same pool-slot budget as regular channels.
        // Without this, a burst of WaitForConfirm=true publishes can open unbounded channels.
        await _poolLimit.WaitAsync(ct).ConfigureAwait(false);

        try
        {
            // Publisher-confirm channels must NOT be pooled alongside standard channels.
            // A fresh channel is created per publish call and disposed after use.
            var confirmOptions = new CreateChannelOptions(
                publisherConfirmationsEnabled: true,
                publisherConfirmationTrackingEnabled: true);

            var channel = await CreateNewChannelAsync(confirmOptions, ct).ConfigureAwait(false);

            // Register the lease BEFORE handing it to the caller so DisposeAsync
            // can safely wait for the count to reach zero.
            Interlocked.Increment(ref _activeLeases);
            return new ChannelLease(this, channel, returnToPool: false);
        }
        catch (Exception ex)
        {
            // Release the semaphore slot we reserved so it is not lost forever.
            _poolLimit.Release();
            logger.LogError(ex, "Failed to acquire RabbitMQ confirm channel.");
            throw;
        }
    }

    private async ValueTask ReturnChannelAsync(IChannel channel)
    {
        try
        {
            if (_disposed || !channel.IsOpen)
                await DisposeChannelInternalAsync(channel).ConfigureAwait(false);
            else
                _channelPool.Enqueue(channel);
        }
        finally
        {
            Interlocked.Decrement(ref _activeLeases);

            if (!_disposed)
            {
                try { _poolLimit.Release(); }
                catch (ObjectDisposedException) { }
            }
        }
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

            if (_connection is not null)
            {
                _connection.ConnectionShutdownAsync -= OnConnectionShutdown;
                try
                {
                    if (_connection.IsOpen)
                    {
                        await _connection.CloseAsync(cancellationToken: ct).ConfigureAwait(false);
                    }
                }
                catch
                {
                    // Ignore cleanup failures during reconnect.
                }

                _connection.Dispose();
            }

            _connection = await connectionFactory.CreateConnectionAsync(ct).ConfigureAwait(false);
            _connection.ConnectionShutdownAsync += OnConnectionShutdown;

            logger.LogInformation("RabbitMQ Client connected to {Endpoint}", _connection.Endpoint);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to connect to RabbitMQ node.");
            throw;
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
    private async ValueTask<IChannel> CreateNewChannelAsync(
        CreateChannelOptions? options,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(_connection);
        return await _connection.CreateChannelAsync(options, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Purges all channels currently residing in the pool and ensures they are properly closed and disposed.
    /// </summary>
    private async ValueTask CleanupPoolAsync()
    {
        while (_channelPool.TryDequeue(out var oldChannel))
            await DisposeChannelInternalAsync(oldChannel).ConfigureAwait(false);
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

        // Wait for all outstanding leases to be returned before disposing the semaphore.
        // This prevents ObjectDisposedException in concurrent ReturnChannelAsync calls.
        // A 30-second timeout guards against leaked leases (e.g. unhandled exceptions in consumers).
        const int drainTimeoutMs = 30_000;
        const int pollIntervalMs = 10;
        var elapsed = 0;

        while (Volatile.Read(ref _activeLeases) > 0)
        {
            if (elapsed >= drainTimeoutMs)
            {
                logger.LogWarning(
                    "PersistentConnection disposed with {Count} outstanding lease(s) still active after {Timeout}ms. " +
                    "Semaphores will be disposed; callers may observe ObjectDisposedException.",
                    _activeLeases, drainTimeoutMs);
                break;
            }

            await Task.Delay(pollIntervalMs).ConfigureAwait(false);
            elapsed += pollIntervalMs;
        }

        await CleanupPoolAsync().ConfigureAwait(false);

        if (_connection is not null)
        {
            _connection.ConnectionShutdownAsync -= OnConnectionShutdown;
            try
            {
                if (_connection.IsOpen)
                    await _connection.CloseAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Ignoring RabbitMQ connection close failure during disposal.");
            }
            finally
            {
                _connection.Dispose();
            }
        }

        _poolLimit.Dispose();
        _connectionLock.Dispose();
    }

    /// <summary>
    /// Disposes a non-pooled (confirm) channel and releases the pool slot +
    /// active-lease counter that were acquired in <see cref="AcquireConfirmChannelAsync"/>.
    /// </summary>
    private async ValueTask ReleaseConfirmChannelAsync(IChannel channel)
    {
        try
        {
            // Always dispose; confirm channels are never returned to the pool.
            await DisposeChannelInternalAsync(channel).ConfigureAwait(false);
        }
        finally
        {
            Interlocked.Decrement(ref _activeLeases);

            if (!_disposed)
            {
                try { _poolLimit.Release(); }
                catch (ObjectDisposedException) { }
            }
        }
    }


    /// <summary>
    /// A lightweight wrapper for an <see cref="IChannel"/> that either returns the channel
    /// to the pool or disposes it depending on <paramref name="returnToPool"/>.
    /// </summary>
    /// <param name="connection">The parent persistent connection managing the pool.</param>
    /// <param name="channel">The acquired RabbitMQ channel.</param>
    public readonly struct ChannelLease(
        PersistentConnection connection,
        IChannel channel,
        bool returnToPool) : IAsyncDisposable
    {
        /// <summary>
        /// Gets the RabbitMQ channel associated with this lease.
        /// </summary>
        public IChannel Channel { get; } = channel;

        /// <summary>
        /// Returns the channel to the pool asynchronously.
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            if (returnToPool)
                await connection.ReturnChannelAsync(Channel).ConfigureAwait(false);
            else
                await connection.ReleaseConfirmChannelAsync(Channel).ConfigureAwait(false);
        }
    }
}
