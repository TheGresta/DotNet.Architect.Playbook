using System.Diagnostics;

using Playbook.Persistence.HybridCaching.Core.Providers;
using Playbook.Persistence.HybridCaching.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Registers the high-performance caching infrastructure defined in the infrastructure extensions.
builder.Services.AddPlaybookCaching(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

/// <summary>
/// Executes a benchmark test to retrieve a large collection of products.
/// </summary>
/// <param name="type">The serialization strategy to test: "smart" (Protobuf + Brotli) or "casual" (JSON/Default).</param>
/// <param name="sp">The service provider to resolve scoped providers.</param>
/// <param name="logger">The logger to record benchmark results.</param>
/// <param name="ct">The cancellation token for the request.</param>
app.MapGet("/benchmark/{type}", async (string type, IServiceProvider sp, ILogger<Program> logger, CancellationToken ct) =>
{
    var sw = Stopwatch.StartNew();
    object result;

    // Direct service resolution based on the benchmark route parameter.
    if (type == "smart")
    {
        var provider = sp.GetRequiredService<IProductProvider>();
        result = await provider.GetProductsAsync(ct);
    }
    else
    {
        var provider = sp.GetRequiredService<ICasualWayProductProvider>();
        result = await provider.GetProductsAsync(ct);
    }

    sw.Stop();

    // Reflection-based count extraction for logging purposes.
    var count = (result as System.Collections.ICollection)?.Count ?? 0;

    logger.LogInformation($"[Benchmark] {type.ToUpper()} took {sw.ElapsedMilliseconds}ms for {count} items.");

    return Results.Ok(new { Type = type, ElapsedMs = sw.ElapsedMilliseconds, Count = count });
});

/// <summary>
/// Triggers a cache invalidation for the specified product list type.
/// </summary>
/// <param name="type">The category to invalidate: "smart" or "casual".</param>
/// <param name="sp">The service provider.</param>
/// <param name="ct">The cancellation token.</param>
app.MapPost("/benchmark/reload/{type}", async (string type, IServiceProvider sp, CancellationToken ct) =>
{
    // Utilizes the ICacheProvider.NotifyInvalidationAsync logic to clear tags in the L2 cache.
    if (type == "smart")
        await sp.GetRequiredService<IProductProvider>().ReloadProductsAsync(ct);
    else
        await sp.GetRequiredService<ICasualWayProductProvider>().ReloadProductsAsync(ct);

    return Results.Accepted();
});

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
