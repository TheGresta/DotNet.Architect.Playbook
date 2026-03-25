using Playbook.Messaging.RabbitMQ.Handlers;
using Playbook.Messaging.RabbitMQ.Messaging.Abstractions;
using Playbook.Messaging.RabbitMQ.Messaging.Configuration;
using Playbook.Messaging.RabbitMQ.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var rabbitSection = builder.Configuration.GetSection("RabbitMq");

builder.Services.AddRabbitMessaging(rabbitSection.Bind)
.AddProducer<OrderCreated>(setup => setup
    .ToExchange("orders.v1.exchange")
    .WithDeadLetter("orders.v1.error.exchange")
    .WithTTL(TimeSpan.FromDays(1))
)
.AddConsumer<OrderCreated>(config =>
{
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

// Simple endpoint to test the Producer
app.MapPost("/orders", async (IProducer<OrderCreated> producer) =>
{
    var order = new OrderCreated(Guid.NewGuid(), 99.99m, DateTime.UtcNow);

    // Test Single Publish
    await producer.PublishAsync(order);

    return Results.Accepted();
});

app.MapControllers();

app.Run();
