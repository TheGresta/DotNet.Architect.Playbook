using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Channels;

using Playbook.Messaging.RabbitMQ.Messaging.Abstractions;
using Playbook.Messaging.RabbitMQ.Messaging.Configuration;
using Playbook.Messaging.RabbitMQ.Messaging.Internal;
using Playbook.Messaging.RabbitMQ.Messaging.Internal.Serialization;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Playbook.Messaging.RabbitMQ.Messaging.Engine.Consumer;

internal sealed class RabbitConsumerEngine<T>(
    PersistentConnection connection,
    RabbitOptions options,
    ConsumerRegistry consumerRegistry,
    MessageEndpointRegistry endpointRegistry,
    IServiceScopeFactory scopeFactory,
    ILogger<RabbitConsumerEngine<T>> logger) : BackgroundService where T : class
{
    private readonly Channel<MessageContext> _buffer = Channel.CreateBounded<MessageContext>(options.PrefetchCount);

    // Internal wrapper to carry RabbitMQ metadata to the workers
    private record MessageContext(ReadOnlyMemory<byte> Body, ulong DeliveryTag, IChannel Channel);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var handlers = consumerRegistry.GetHandlersForType(typeof(T)).ToList();
        logger.LogInformation("Consumer for {Type} starting with {Count} handlers.", typeof(T).Name, handlers.Count);

        if (handlers.Count == 0)
        {
            logger.LogWarning("No handlers registered for {Type}. Consumer will not start.", typeof(T).Name);
            return;
        }

        // 1. Start Workers - Do NOT await them yet
        var workerTasks = Enumerable.Range(0, options.MaxConcurrency)
            .Select(_ => WorkerAsync(stoppingToken))
            .ToList();

        // 2. Start the Consumer - Do NOT await the internal Delay here
        // We wrap it in Task.Run or just ensure it returns a Task that represents the "Lifetime"
        var consumerTask = StartConsumerAsync(stoppingToken);

        // 3. Wait specifically for the SHUTDOWN signal
        // This task completes as soon as the OS tells the app to stop
        await Task.Delay(Timeout.Infinite, stoppingToken).ContinueWith(_ => { });

        logger.LogInformation("Shutdown signal received for {Type}. Draining buffer...", typeof(T).Name);

        // 4. Close the producer side of the internal buffer
        // This tells the workers: "No more messages are coming, exit when you finish the current ones."
        _buffer.Writer.TryComplete();

        // 5. Wait for all workers to finish their current job
        await Task.WhenAll(workerTasks);

        logger.LogInformation("All workers for {Type} have finished. Shutdown complete.", typeof(T).Name);
    }

    private async Task StartConsumerAsync(CancellationToken ct)
    {
        var channel = await connection.GetChannelAsync(ct);
        var definition = endpointRegistry.GetDefinition<T>();
        var queueName = $"{typeof(T).Name}.Queue";

        if (!string.IsNullOrEmpty(definition.DeadLetterExchange))
        {
            await channel.ExchangeDeclareAsync(
                exchange: definition.DeadLetterExchange,
                type: ExchangeType.Fanout,
                durable: true,
                cancellationToken: ct);

            // Optional: Declare the Error Queue and bind it automatically
            var errorQueue = $"{queueName}.Error";
            await channel.QueueDeclareAsync(errorQueue, true, false, false, cancellationToken: ct);
            await channel.QueueBindAsync(errorQueue, definition.DeadLetterExchange, string.Empty, cancellationToken: ct);
        }

        // 2. Add DLX Arguments to the MAIN Queue
        var args = new Dictionary<string, object?>();
        if (!string.IsNullOrEmpty(definition.DeadLetterExchange))
        {
            args.Add("x-dead-letter-exchange", definition.DeadLetterExchange);
            args.Add("x-dead-letter-routing-key", definition.DeadLetterRoutingKey);
        }

        // 1. Ensure the exchange exists (High Speed: idempotent call)
        await channel.ExchangeDeclareAsync(
            exchange: definition.ExchangeName,
            type: ExchangeType.Fanout,
            durable: true,
            cancellationToken: ct);

        // 2. Ensure the queue exists
        await channel.QueueDeclareAsync(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: args,
            cancellationToken: ct);

        // 3. Bind the queue to the exchange
        await channel.QueueBindAsync(queueName, definition.ExchangeName, string.Empty, cancellationToken: ct);

        // 4. Set Quality of Service (Backpressure)
        await channel.BasicQosAsync(0, (ushort)options.PrefetchCount, false, ct);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            // If we are shutting down, don't accept new items into the internal buffer
            if (_buffer.Writer.TryWrite(new MessageContext(ea.Body.ToArray(), ea.DeliveryTag, channel)))
            {
                return;
            }

            // If buffer is full or closing, Nack and requeue so another pod takes it
            await channel.BasicNackAsync(ea.DeliveryTag, false, true, CancellationToken.None);
        };

        var consumerTag = await channel.BasicConsumeAsync(queueName, false, consumer, ct);

        logger.LogInformation("Successfully started RabbitMQ consumer for {Exchange} -> {Queue}", definition.ExchangeName, queueName);

        // Wait until shutdown
        await Task.Delay(Timeout.Infinite, ct).ContinueWith(async _ =>
        {
            // Explicitly tell RabbitMQ we are done listening
            await channel.BasicCancelAsync(consumerTag, false, CancellationToken.None);
        });
    }

    private async Task WorkerAsync(CancellationToken ct)
    {
        logger.LogInformation("Worker thread started for {Type}. Capacity: {Max}", typeof(T).Name, options.MaxConcurrency);

        // We do NOT pass 'ct' to ReadAllAsync. 
        // We want the loop to continue until _buffer.Writer.TryComplete() is called 
        // AND the buffer is empty.
        await foreach (var ctx in _buffer.Reader.ReadAllAsync(CancellationToken.None))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var typeInfo = (JsonTypeInfo<T>)MessagingJsonContext.Default.GetTypeInfo(typeof(T))!;
                var message = JsonSerializer.Deserialize(ctx.Body.Span, typeInfo);

                if (message != null)
                {
                    var handlerTypes = consumerRegistry.GetHandlersForType(typeof(T));
                    var tasks = handlerTypes.Select(handlerType =>
                    {
                        var handler = (IIntegrationEventHandler<T>)scope.ServiceProvider.GetRequiredService(handlerType);
                        // Pass the 'ct' here so the handler can abort internal DB calls if it wants
                        return handler.HandleAsync(message, ct);
                    });

                    await Task.WhenAll(tasks);

                    // Use None here to ensure the ACK reaches RabbitMQ even during shutdown
                    await ctx.Channel.BasicAckAsync(ctx.DeliveryTag, false, CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during graceful shutdown processing for {Tag}", ctx.DeliveryTag);
                // Move to DLX if it fails during shutdown
                await ctx.Channel.BasicNackAsync(ctx.DeliveryTag, false, false, CancellationToken.None);
            }
        }
    }
}
