using MassTransit;

using Microsoft.EntityFrameworkCore;

using Playbook.Messaging.MassTransit.Application.Services;
using Playbook.Messaging.MassTransit.Contracts;
using Playbook.Messaging.MassTransit.Infrastructure.Messaging;
using Playbook.Messaging.MassTransit.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// 1. Bind Options
// Registers the message broker connection settings into the IOptions pattern for injection-friendly access.
builder.Services.Configure<MessageBusOptions>(
    builder.Configuration.GetSection(MessageBusOptions.SectionName));

// 2. Database Setup
// Configures PostgreSQL as the primary data store for both application data and Saga state persistence.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 3. Application Services
// Registers the ChaosService to simulate transient failures and validate the resilience of the saga's compensation logic.
builder.Services.AddScoped<IChaosService, ChaosService>();

// 4. Register MassTransit (Using our Extension Method from Step 2)
// This encapsulates the complex setup of the State Machine, Consumers, and RabbitMQ transport.
builder.Services.AddEnterpriseMessaging(builder.Configuration);

var app = builder.Build();

// 5. Automatic Migration (Handy for Demos/Dev)
// Ensures the database schema, including Saga state and Outbox tables, is up-to-date on startup.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// 6. The "Chaos Trigger" Endpoint
// Serves as the entry point for the distributed workflow, generating a unique CorrelationId 
// to track the entire lifecycle of the request across various microservices.
app.MapPost("/workflow/start", async (string name, IPublishEndpoint publishEndpoint) =>
{
    var correlationId = Guid.NewGuid();

    // We publish the command to start the Saga. 
    // Using IPublishEndpoint ensures the message is routed to the correct exchange/queue defined in the State Machine.
    await publishEndpoint.Publish(new StartWorkflow(correlationId, name));

    return Results.Accepted($"/workflow/status/{correlationId}", new
    {
        Message = "Workflow Initiated",
        TrackingId = correlationId
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
