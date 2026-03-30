using System.Diagnostics;

using Playbook.Persistence.HybridCaching.Core.Providers;
using Playbook.Persistence.HybridCaching.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddPlaybookCaching(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/benchmark/{type}", async (string type, IServiceProvider sp, ILogger<Program> logger, CancellationToken ct) =>
{
    var sw = Stopwatch.StartNew();
    object result;

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
    var count = (result as System.Collections.ICollection)?.Count ?? 0;

    logger.LogInformation($"[Benchmark] {type.ToUpper()} took {sw.ElapsedMilliseconds}ms for {count} items.");

    return Results.Ok(new { Type = type, ElapsedMs = sw.ElapsedMilliseconds, Count = count });
});

app.MapPost("/benchmark/reload/{type}", async (string type, IServiceProvider sp, CancellationToken ct) =>
{
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
