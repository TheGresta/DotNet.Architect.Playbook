using Playbook.Messaging.RabbitMQ.Handlers;
using Playbook.Messaging.RabbitMQ.Messaging.Abstractions;
using Playbook.Messaging.RabbitMQ.Messaging.Configuration;
using Playbook.Messaging.RabbitMQ.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// --- RabbitMQ Infrastructure Configuration ---
// Retrieve the configuration section and bind it to the RabbitOptions model.
// This fluently registers the core messaging engine, including connection pooling and topology management.
var rabbitSection = builder.Configuration.GetSection("RabbitMq");

builder.Services.AddRabbitMessaging(rabbitSection.Bind)
    // Register a Typed Producer for the OrderCreated event.
    // Configuration includes custom exchange naming, Dead Letter Exchange (DLX) routing, and message TTL.
    .AddProducer<OrderCreated>(setup => setup
        .ToExchange("orders.v1.exchange")
        .WithDeadLetter("orders.v1.error.exchange")
        .WithTtl(TimeSpan.FromDays(1))
    )
    // Register a Concurrent Consumer for the OrderCreated event.
    // This automatically starts a background engine that dispatches incoming messages to multiple handlers in parallel.
    .AddConsumer<OrderCreated>(config =>
    {
        // Handlers are resolved via DI and executed within a discrete asynchronous scope.
        config.AddHandler<InventoryUpdateHandler>();
        config.AddHandler<NotificationHandler>();
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

// --- Minimal API Integration ---
/// <summary>
/// A test endpoint to demonstrate the usage of the strongly-typed <see cref="IProducer{T}"/>.
/// Publishes an <see cref="OrderCreated"/> message to the configured RabbitMQ exchange.
/// </summary>
app.MapPost("/orders", async (IProducer<OrderCreated> producer) =>
{
    var order = new OrderCreated(Guid.NewGuid(), 99.99m, DateTime.UtcNow);

    // Test Single Publish
    await producer.PublishAsync(order);

    return Results.Accepted();
});

app.MapControllers();

app.Run();
