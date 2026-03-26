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
builder.Services.Configure<MessageBusOptions>(
    builder.Configuration.GetSection(MessageBusOptions.SectionName));

// 2. Database Setup
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 3. Application Services
builder.Services.AddScoped<IChaosService, ChaosService>();

// 4. Register MassTransit (Using our Extension Method from Step 2)
// Note: Ensure your Extension Method is updated to use IOptions<MessageBusOptions>
builder.Services.AddEnterpriseMessaging(builder.Configuration);

var app = builder.Build();

// 5. Automatic Migration (Handy for Demos/Dev)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// 6. The "Chaos Trigger" Endpoint
app.MapPost("/workflow/start", async (string name, IPublishEndpoint publishEndpoint) =>
{
    var correlationId = Guid.NewGuid();

    // We publish the command to start the Saga
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
