using Microsoft.AspNetCore.SignalR.Client;

using Playbook.Messaging.SignalR.Domain.Models;

namespace Playbook.Messaging.SignalR.Terminal;

public sealed class ManualTestClient : BackgroundService
{
    private readonly string _hubUrl = "http://localhost:5190/stockHub";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Define our two distinct traders
        var traderA = StartTraderAsync("TRADER_ALICE", ["BTC", "ETH", "TSLA"], stoppingToken);
        var traderB = StartTraderAsync("TRADER_BOB", ["AAPL", "MSFT", "ETH"], stoppingToken);

        await Task.WhenAll(traderA, traderB);
    }

    private async Task StartTraderAsync(string name, string[] myStocks, CancellationToken ct)
    {
        var connection = new HubConnectionBuilder()
            .WithUrl(_hubUrl)
            .AddMessagePackProtocol()
            .WithAutomaticReconnect()
            .Build();

        // 1. Setup Structured Logging for this specific client
        connection.On<StockPrice>("ReceivePriceUpdate", (price) =>
        {
            // Use colors to differentiate in the console
            var color = name == "TRADER_ALICE" ? ConsoleColor.Cyan : ConsoleColor.Magenta;
            lock (Console.Out) // Prevent interlaced text in high-frequency streams
            {
                Console.ForegroundColor = color;
                Console.WriteLine($"[{name}] TICK | {price.Symbol.PadRight(5)} | ${price.Price:F2}");
                Console.ResetColor();
            }
        });

        connection.On<string>("ReceiveNotification", (msg) =>
            Console.WriteLine($"[{name}] SYSTEM: {msg}"));

        try
        {
            Console.WriteLine($"[{name}] Attempting connection...");
            await connection.StartAsync(ct);
            Console.WriteLine($"[{name}] Connected! Protocol: MessagePack");

            // 2. Batch Subscribe
            foreach (var symbol in myStocks)
            {
                await connection.InvokeAsync("SubscribeToStock", symbol, ct);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{name}] Critical Failure: {ex.Message}");
        }

        // Keep this specific trader's task alive
        await Task.Delay(-1, ct);
    }
}
