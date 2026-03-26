using MassTransit;

using Playbook.Messaging.MassTransit.Courier.Messaging;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// 1. Register Chaos Provider
builder.Services.AddScoped<IChaosProvider, ChaosProvider>();

// 2. Configure MassTransit
builder.Services.AddMassTransit(x =>
{
    x.AddActivities(typeof(Program).Assembly);
    x.AddConsumer<RoutingSlipMetricsConsumer>();

    x.UsingInMemory((context, cfg) =>
    {
        // Global Retry Policy for all consumers/activities
        cfg.UseMessageRetry(r => r.Immediate(3));

        // Setup the specific endpoints for our activities
        cfg.ConfigureEndpoints(context);

        // Customizing the Activity Endpoints to handle Compensation Retries
        // Note: In a real app, you'd use RabbitMQ exchange names here.
    });
});

var app = builder.Build();

// Simple health check endpoint
app.MapGet("/", () => "Courier Orchestrator is Online.");

app.MapPost("/start-workflow", async (IBus bus) =>
{
    var transactionId = Guid.NewGuid();
    var trackingNumber = NewId.NextGuid();

    // Create the "Itinerary"
    var builder = new RoutingSlipBuilder(trackingNumber);

    // Step 1: Add State One
    builder.AddActivity("StateOne", new Uri("exchange:StateOne_execute"), new
    {
        TransactionId = transactionId,
        Data = "Sample Payload"
    });

    // Step 2: Add State Two
    builder.AddActivity("StateTwo", new Uri("exchange:StateTwo_execute"), new
    {
        TransactionId = transactionId
    });

    // Step 3: Add State Three
    builder.AddActivity("StateThree", new Uri("exchange:StateThree_execute"), new
    {
        TransactionId = transactionId
    });

    var routingSlip = builder.Build();

    // Launch the traveler
    await bus.Execute(routingSlip);

    return Results.Accepted($"/status/{trackingNumber}", new
    {
        TrackingNumber = trackingNumber,
        TransactionId = transactionId
    });
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
