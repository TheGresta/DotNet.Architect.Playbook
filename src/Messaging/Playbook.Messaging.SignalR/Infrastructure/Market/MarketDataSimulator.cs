using Microsoft.AspNetCore.SignalR;

using Playbook.Messaging.SignalR.Domain.Interfaces;
using Playbook.Messaging.SignalR.Domain.Models;
using Playbook.Messaging.SignalR.Infrastructure.RealTime;

namespace Playbook.Messaging.SignalR.Infrastructure.Market;

public sealed class MarketDataSimulator(IHubContext<StockTickerHub, IStockClient> hubContext) : BackgroundService
{
    private readonly string[] _symbols = ["AAPL", "MSFT", "GOOGL", "TSLA", "BTC", "ETH"];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // PeriodicTimer is more precise than Task.Delay for high-frequency ticks
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(100));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            // We use Task.WhenAll to push all symbols in parallel 
            // without waiting for the first one to finish before starting the second.
            var tasks = _symbols.Select(symbol =>
            {
                var price = GenerateMockPrice(symbol);
                return hubContext.Clients.Group(symbol).ReceivePriceUpdate(price);
            });

            await Task.WhenAll(tasks);
        }
    }

    private static StockPrice GenerateMockPrice(string symbol) =>
        new(symbol, Random.Shared.Next(100, 1000), Random.Shared.NextDouble(), DateTime.UtcNow);
}
