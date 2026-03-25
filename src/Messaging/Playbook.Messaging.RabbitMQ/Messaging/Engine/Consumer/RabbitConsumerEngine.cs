using System.Threading.Channels;

using Playbook.Messaging.RabbitMQ.Messaging.Abstractions;
using Playbook.Messaging.RabbitMQ.Messaging.Configuration;
using Playbook.Messaging.RabbitMQ.Messaging.Internal;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Playbook.Messaging.RabbitMQ.Messaging.Engine.Consumer;

internal sealed class RabbitConsumerEngine<T>(
    PersistentConnection connection,
    ITopologyManager topologyManager,
    IMessageDispatcher dispatcher,
    RabbitOptions options,
    ConsumerRegistry consumerRegistry,
    ILogger<RabbitConsumerEngine<T>> logger) : BackgroundService where T : class
{
    private readonly Channel<MessageContext> _buffer = Channel.CreateBounded<MessageContext>(options.PrefetchCount);
    private readonly string _queueName = $"{typeof(T).Name}.Queue";

    private record MessageContext(ReadOnlyMemory<byte> Body, ulong DeliveryTag, IChannel Channel);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var handlers = consumerRegistry.GetHandlersForType(typeof(T)).ToList();
        if (handlers.Count == 0)
        {
            logger.LogWarning("No handlers registered for {Type}. Consumer inhibited.", typeof(T).Name);
            return;
        }

        // 1. Start Worker Pool
        var workerTasks = Enumerable.Range(0, options.MaxConcurrency)
            .Select(_ => Task.Run(() => WorkerAsync(stoppingToken), stoppingToken))
            .ToArray();

        // 2. Start the Consumer Loop
        // We don't use 'await using' on the lease here because the lease must live 
        // as long as the BackgroundService is running to keep the consumer active.
        var lease = await connection.AcquireAsync(stoppingToken).ConfigureAwait(false);
        try
        {
            await topologyManager.EnsureTopologyAsync<T>(lease.Channel, stoppingToken).ConfigureAwait(false);

            await StartConsumerAsync(lease.Channel, stoppingToken).ConfigureAwait(false);

            // 3. Keep the service alive until stoppingToken is signaled
            await Task.Delay(Timeout.Infinite, stoppingToken).ContinueWith(_ => { });
        }
        catch (OperationCanceledException) { /* Normal shutdown */ }
        finally
        {
            logger.LogInformation("Shutdown initiated for {Type}. Draining {Count} buffered messages...",
                typeof(T).Name, _buffer.Reader.Count);

            _buffer.Writer.TryComplete();
            await Task.WhenAll(workerTasks).ConfigureAwait(false);
            await lease.DisposeAsync().ConfigureAwait(false);
        }
    }

    private async Task StartConsumerAsync(IChannel channel, CancellationToken ct)
    {
        await channel.BasicQosAsync(0, (ushort)options.PrefetchCount, false, ct).ConfigureAwait(false);

        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += async (_, ea) =>
        {
            var ctx = new MessageContext(ea.Body.ToArray(), ea.DeliveryTag, channel);

            if (!_buffer.Writer.TryWrite(ctx))
            {
                // Buffer full: Nack and requeue so other instances can pick it up
                await channel.BasicNackAsync(ea.DeliveryTag, false, requeue: true, CancellationToken.None);
            }
        };

        await channel.BasicConsumeAsync(_queueName, autoAck: false, consumer: consumer, cancellationToken: ct)
            .ConfigureAwait(false);

        logger.LogInformation("RabbitMQ Consumer active: {Queue}", _queueName);
    }

    private async Task WorkerAsync(CancellationToken ct)
    {
        // We iterate until TryComplete() is called AND buffer is empty
        await foreach (var ctx in _buffer.Reader.ReadAllAsync(CancellationToken.None))
        {
            try
            {
                await dispatcher.DispatchAsync<T>(ctx.Body, ct).ConfigureAwait(false);
                await ctx.Channel.BasicAckAsync(ctx.DeliveryTag, false, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process message {Tag}. Moving to DLX.", ctx.DeliveryTag);
                // requeue: false triggers the Dead Letter Exchange
                await ctx.Channel.BasicNackAsync(ctx.DeliveryTag, false, requeue: false, CancellationToken.None).ConfigureAwait(false);
            }
        }
    }
}
