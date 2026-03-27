using System.Diagnostics;

using Playbook.Persistence.Meilisearch;
using Playbook.Persistence.Meilisearch.Features.SearchCars;
using Playbook.Persistence.Meilisearch.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Registers the Meilisearch Client, Context, and automatic index configurations.
// Uses the source-generated MeiliJsonContext for zero-reflection serialization.
builder.Services.AddMeiliPersistence(builder.Configuration);

// Feature Handlers and Observability
builder.Services.AddScoped<SearchCarsHandler>();
builder.Services.AddSingleton<SearchLogger>();

var app = builder.Build();

// 2. High-Performance Search Endpoint
// Utilizing Minimal APIs for lower overhead and direct integration with the SearchCarsHandler.
app.MapPost("/search", async (
    SearchCarsRequest request,
    SearchCarsHandler handler,
    SearchLogger logger,
    CancellationToken ct) =>
{
    // Start timing the request for performance telemetry.
    var sw = Stopwatch.StartNew();

    // The handler encapsulates the ICarDocumentRepository and CarSearchSpecs logic.
    var results = await handler.HandleAsync(request, ct);

    sw.Stop();

    // Leverage the partial class SearchLogger with [LoggerMessage] for non-allocating logs.
    logger.LogSearchPerformance(request.SearchTerm, sw.ElapsedMilliseconds, results.TotalCount);

    return Results.Ok(results);
});

// 3. Administrative / DevOps Endpoint
// Trigger this endpoint after deployment to synchronize MeiliFilterable and MeiliSortable 
// attributes from C# records to the Meilisearch engine settings.
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
