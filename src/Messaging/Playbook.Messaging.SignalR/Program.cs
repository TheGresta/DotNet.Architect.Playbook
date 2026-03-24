using Playbook.Messaging.SignalR;
using Playbook.Messaging.SignalR.Infrastructure.RealTime;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

/// <summary>
/// Domain-specific registration: Initializes the FinTech real-time engine, 
/// including SignalR, MessagePack, Redis backplane, and background simulators.
/// </summary>
/// <remarks>
/// The connection string is retrieved from the "Redis:ConnectionString" configuration key, 
/// supporting environment-based overrides for local, staging, and production environments.
/// </remarks>
builder.Services.AddFinTechRealTime(builder.Configuration["Redis:ConnectionString"]);

var app = builder.Build();

/// <summary>
/// Maps the <see cref="StockTickerHub"/> to the designated real-time endpoint.
/// Clients connect to this URL to subscribe to live stock symbol updates.
/// </summary>
app.MapHub<StockTickerHub>("/stockHub");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
