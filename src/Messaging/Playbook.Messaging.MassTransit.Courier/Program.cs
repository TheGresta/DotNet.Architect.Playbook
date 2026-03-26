using MassTransit;

using Playbook.Messaging.MassTransit.Courier.Messaging;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

/// <summary>
/// 1. Register Chaos Provider
/// Registers the resilience testing infrastructure to the dependency injection container.
/// </summary>
builder.Services.AddScoped<IChaosProvider, ChaosProvider>();

/// <summary>
/// 2. Configure MassTransit
/// Initializes the MassTransit service bus with Courier activities and metrics consumers.
/// </summary>
builder.Services.AddMassTransit(x =>
{
    // Automatically discovers and registers all IActivity and IExecuteActivity types in the current assembly.
    x.AddActivities(typeof(Program).Assembly);

    // Registers the consumer responsible for tracking workflow completion and failure events.
    x.AddConsumer<RoutingSlipMetricsConsumer>();

    x.UsingInMemory((context, cfg) =>
    {
        /// <summary>
        /// Global Retry Policy
        /// Configures a simple immediate retry strategy. In production, an exponential backoff 
        /// is recommended to handle transient network or resource contention issues.
        /// </summary>
        cfg.UseMessageRetry(r => r.Immediate(3));

        // Automatically configures endpoints for all registered activities and consumers using default naming conventions.
        cfg.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

/// <summary>
/// Simple health check endpoint to verify the orchestrator's availability.
/// </summary>
app.MapGet("/", () => "Courier Orchestrator is Online.");

/// <summary>
/// Workflow Initiation Endpoint
/// Orchestrates the creation of a Routing Slip (Itinerary) and dispatches it for execution.
/// </summary>
app.MapPost("/start-workflow", async (IBus bus) =>
{
    var transactionId = Guid.NewGuid();
    var trackingNumber = NewId.NextGuid();

    /// <summary>
    /// Create the "Itinerary"
    /// The RoutingSlipBuilder defines the sequence of activities (forward) and implicitly 
    /// tracks the compensation requirements (backward) for the distributed transaction.
    /// </summary>
    var builder = new RoutingSlipBuilder(trackingNumber);

    // Step 1: Add State One
    // Configures the first activity with its required arguments and execution address.
    builder.AddActivity("StateOne", new Uri("exchange:StateOne_execute"), new
    {
        TransactionId = transactionId,
        Data = "Sample Payload"
    });

    // Step 2: Add State Two
    // Configures the second activity. If this fails, State One's compensation logic will be triggered.
    builder.AddActivity("StateTwo", new Uri("exchange:StateTwo_execute"), new
    {
        TransactionId = transactionId
    });

    // Step 3: Add State Three
    // Terminal activity. If this fails, both State Two and State One will compensate in reverse order.
    builder.AddActivity("StateThree", new Uri("exchange:StateThree_execute"), new
    {
        TransactionId = transactionId
    });

    // Builds the immutable Routing Slip instance representing the specific workflow execution path.
    var routingSlip = builder.Build();

    // Dispatches the routing slip to the first activity in the itinerary.
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
