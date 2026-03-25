using System.Threading.Channels;

using Playbook.Messaging.RabbitMQ.Messaging.Abstractions;
using Playbook.Messaging.RabbitMQ.Messaging.Configuration;
using Playbook.Messaging.RabbitMQ.Messaging.Internal;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Playbook.Messaging.RabbitMQ.Messaging.Engine.Consumer;

/// <summary>
/// A high-performance, concurrent background consumer engine for RabbitMQ.
/// Manages a worker pool, internal buffering via <see cref="Channel{T}"/>, and message acknowledgment 
/// logic to ensure reliable processing of message type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The message contract type this engine is responsible for consuming.</typeparam>
/// <remarks>
/// This engine implements a decoupled architecture where the RabbitMQ consumer thread simply pushes 
/// messages into an internal bounded buffer, and a separate pool of workers processes them in parallel.
/// </remarks>
internal sealed class RabbitConsumerEngine<T>(
    PersistentConnection connection,
    ITopologyManager topologyManager,
    IMessageDispatcher dispatcher,
    RabbitOptions options,
    ConsumerRegistry consumerRegistry,
    ILogger<RabbitConsumerEngine<T>> logger) : BackgroundService where T : class
{
    // Internal bounded buffer to decouple network I/O from message processing logic
    private readonly Channel<MessageContext> _buffer = Channel.CreateBounded<MessageContext>(options.PrefetchCount);
    private readonly string _queueName = $"{typeof(T).Name}.Queue";

    /// <summary>
    /// Represents the contextual state of a received message required for downstream processing and acknowledgment.
    /// </summary>
    private record MessageContext(ReadOnlyMemory<byte> Body, ulong DeliveryTag, IChannel Channel);

    /// <summary>
    /// The primary execution loop of the background service. Sets up the worker pool and initializes the consumer.
    /// </summary>
    /// <param name="stoppingToken">Triggered when the host is shutting down.</param>
    /// <returns>A <see cref="Task"/> representing the lifetime of the service.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var handlers = consumerRegistry.GetHandlersForType(typeof(T)).ToList();
        if (handlers.Count == 0)
        {
            logger.LogWarning("No handlers registered for {Type}. Consumer inhibited.", typeof(T).Name);
            return;
        }

        // 1. Start Worker Pool: Initialize a fixed number of workers to process the internal buffer in parallel
        var workerTasks = Enumerable.Range(0, options.MaxConcurrency)
            .Select(_ => Task.Run(() => WorkerAsync(stoppingToken), stoppingToken))
            .ToArray();

        // 2. Start the Consumer Loop
        // We manually manage this lease to ensure the consumer's channel stays open for the service duration
        var lease = await connection.AcquireAsync(stoppingToken).ConfigureAwait(false);
        try
        {
            await topologyManager.EnsureTopologyAsync<T>(lease.Channel, stoppingToken).ConfigureAwait(false);

            await StartConsumerAsync(lease.Channel, stoppingToken).ConfigureAwait(false);

            // 3. Keep the service alive until stoppingToken is signaled
            await Task.Delay(Timeout.Infinite, stoppingToken).ContinueWith(_ => { });
        }
        catch (OperationCanceledException) { /* Standard exit path on shutdown */ }
        finally
        {
            logger.LogInformation("Shutdown initiated for {Type}. Draining {Count} buffered messages...",
                typeof(T).Name, _buffer.Reader.Count);

            // Signal the buffer that no more items will be written, allowing workers to drain the queue
            _buffer.Writer.TryComplete();
            await Task.WhenAll(workerTasks).ConfigureAwait(false);
            await lease.DisposeAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Configures the RabbitMQ consumer and attaches the asynchronous receive event handler.
    /// </summary>
    private async Task StartConsumerAsync(IChannel channel, CancellationToken ct)
    {
        // Set PrefetchCount to control how many unacknowledged messages the broker sends to this instance
        await channel.BasicQosAsync(0, (ushort)options.PrefetchCount, false, ct).ConfigureAwait(false);

        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += async (_, ea) =>
        {
            var ctx = new MessageContext(ea.Body.ToArray(), ea.DeliveryTag, channel);

            // Non-blocking attempt to write to the internal buffer
            if (!_buffer.Writer.TryWrite(ctx))
            {
                // If the buffer is full (backpressure), Nack and requeue so other nodes can process it
                await channel.BasicNackAsync(ea.DeliveryTag, false, requeue: true, CancellationToken.None);
            }
        };

        await channel.BasicConsumeAsync(_queueName, autoAck: false, consumer: consumer, cancellationToken: ct)
            .ConfigureAwait(false);

        logger.LogInformation("RabbitMQ Consumer active: {Queue}", _queueName);
    }

    /// <summary>
    /// A background worker that pulls messages from the internal buffer and invokes the dispatcher.
    /// </summary>
    private async Task WorkerAsync(CancellationToken ct)
    {
        // ReadAllAsync continues until the buffer is marked as complete and empty
        await foreach (var ctx in _buffer.Reader.ReadAllAsync(CancellationToken.None))
        {
            try
            {
                await dispatcher.DispatchAsync<T>(ctx.Body, ct).ConfigureAwait(false);

                // Explicit Ack upon successful processing of all handlers
                await ctx.Channel.BasicAckAsync(ctx.DeliveryTag, false, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process message {Tag}. Moving to DLX.", ctx.DeliveryTag);

                // Nack with requeue: false to route the message to the Dead Letter Exchange (DLX) for inspection
                await ctx.Channel.BasicNackAsync(ctx.DeliveryTag, false, requeue: false, CancellationToken.None).ConfigureAwait(false);
            }
        }
    }
}
