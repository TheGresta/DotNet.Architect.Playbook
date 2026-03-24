using Microsoft.AspNetCore.SignalR;

using Playbook.Messaging.SignalR.Domain.Interfaces;
using Playbook.Messaging.SignalR.Domain.Models;
using Playbook.Messaging.SignalR.Infrastructure.RealTime;

namespace Playbook.Messaging.SignalR.Infrastructure.Market;

public sealed class MarketDataSimulator(IHubContext<StockTickerHub, IStockClient> hubContext) : BackgroundService
{
    private static readonly string[] _symbols = ["AAPL", "MSFT", "GOOGL", "TSLA", "BTC", "ETH"];
    private static readonly TimeSpan _tickInterval = TimeSpan.FromMilliseconds(100);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_tickInterval);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                // Optimization: Avoid LINQ overhead and array allocations in a high-frequency (100ms) loop.
                var tasks = new Task[_symbols.Length];

                for (var i = 0; i < _symbols.Length; i++)
                {
                    var symbol = _symbols[i];
                    var price = GenerateMockPrice(symbol);
                    tasks[i] = hubContext.Clients.Group(symbol).ReceivePriceUpdate(price);
                }

                await Task.WhenAll(tasks);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected shutdown behavior
        }
    }

    private static StockPrice GenerateMockPrice(string symbol) =>
        new(symbol,
            Random.Shared.Next(100, 1000),
            Random.Shared.NextDouble(),
            DateTime.UtcNow);
}
