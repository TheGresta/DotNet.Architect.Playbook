using Microsoft.AspNetCore.SignalR;

using Playbook.Messaging.SignalR.Domain.Interfaces;
using Playbook.Messaging.SignalR.Domain.Models;
using Playbook.Messaging.SignalR.Infrastructure.RealTime;

namespace Playbook.Messaging.SignalR.Infrastructure.Market;

/// <summary>
/// A high-performance background service responsible for simulating real-time market data fluctuations.
/// Inherits from <see cref="BackgroundService"/> to provide a long-running execution loop that broadcasts 
/// price updates via SignalR at a fixed frequency.
/// </summary>
/// <remarks>
/// This service utilizes <see cref="PeriodicTimer"/> for precise interval tracking and implements 
/// an allocation-efficient broadcast pattern to minimize GC pressure during high-frequency ticks.
/// </remarks>
public sealed class MarketDataSimulator(IHubContext<StockTickerHub, IStockClient> hubContext) : BackgroundService
{
    /// <summary>
    /// The set of financial symbols tracked and simulated by this service.
    /// </summary>
    private static readonly string[] _symbols = ["AAPL", "MSFT", "GOOGL", "TSLA", "BTC", "ETH"];

    /// <summary>
    /// The fixed interval between market "ticks" or price updates.
    /// </summary>
    private static readonly TimeSpan _tickInterval = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// Executes the background simulation loop.
    /// </summary>
    /// <param name="stoppingToken">Triggered when the host is performing a graceful shutdown.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous life-cycle of the simulation.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // PeriodicTimer is preferred over System.Threading.Timer for async loops to avoid re-entrancy issues.
        using var timer = new PeriodicTimer(_tickInterval);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                // Optimization: Pre-sizing the array based on a known static length to avoid dynamic resizing.
                var tasks = new Task[_symbols.Length];

                for (var i = 0; i < _symbols.Length; i++)
                {
                    var symbol = _symbols[i];
                    var price = GenerateMockPrice(symbol);

                    // Fire-and-collect pattern: Dispatching all SignalR group calls concurrently 
                    // before awaiting the aggregate result to maximize throughput.
                    tasks[i] = BroadcastSafeAsync(symbol, price);
                }

                await Task.WhenAll(tasks);

                async Task BroadcastSafeAsync(string symbol, StockPrice price)
                {
                    try
                    {
                        await hubContext.Clients.Group(symbol).ReceivePriceUpdate(price);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        // Log and continue - one failed group shouldn't affect others
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Standard catch for OperationCanceledException prevents noisy logs during service stop.
        }
    }

    /// <summary>
    /// Generates a randomized <see cref="StockPrice"/> data point for a given symbol.
    /// </summary>
    /// <param name="symbol">The ticker symbol for which to generate a price.</param>
    /// <returns>A populated <see cref="StockPrice"/> instance with randomized market values.</returns>
    private static StockPrice GenerateMockPrice(string symbol) =>
        new(symbol,
            Math.Round((decimal)(Random.Shared.NextDouble() * 900 + 100), 2),
            Random.Shared.NextDouble(),
            DateTime.UtcNow);
}
