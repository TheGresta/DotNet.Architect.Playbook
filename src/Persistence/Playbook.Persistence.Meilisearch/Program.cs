using System.Diagnostics;

using Playbook.Persistence.Meilisearch;
using Playbook.Persistence.Meilisearch.Features.SearchCars;
using Playbook.Persistence.Meilisearch.Infrastructure.Client;
using Playbook.Persistence.Meilisearch.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Add Infrastructure Layer
builder.Services.AddMeiliPersistence(builder.Configuration);

// Register Feature Handlers
builder.Services.AddScoped<SearchCarsHandler>();
builder.Services.AddSingleton<SearchLogger>();

var app = builder.Build();

// Performance-First Search Endpoint
app.MapPost("/search", async (
    SearchCarsRequest request,
    SearchCarsHandler handler,
    SearchLogger logger,
    CancellationToken ct) =>
{
    var sw = Stopwatch.StartNew();

    var results = await handler.HandleAsync(request, ct);

    sw.Stop();

    // Use the Source-Generated Logger for zero-allocation performance
    logger.LogSearchPerformance(request.SearchTerm, sw.ElapsedMilliseconds, results.Hits.Count());

    return Results.Ok(results);
});

// Admin endpoint to sync settings
app.MapPost("/setup", async (MeiliContext context) =>
{
    await context.InitializeSettingsAsync();
    return Results.Ok("Settings Synchronized.");
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
