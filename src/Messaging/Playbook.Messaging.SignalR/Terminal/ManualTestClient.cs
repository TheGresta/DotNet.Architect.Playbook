using Microsoft.AspNetCore.SignalR.Client;

using Playbook.Messaging.SignalR.Domain.Models;

namespace Playbook.Messaging.SignalR.Terminal;

public sealed class ManualTestClient : BackgroundService
{
    private const string _hubUrl = "http://localhost:5190/stockHub";

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Use Task.Run to ensure these start on the thread pool immediately
        var traderA = StartTraderAsync("ALICE", ["BTC", "ETH", "TSLA"], ConsoleColor.Cyan, stoppingToken);
        var traderB = StartTraderAsync("BOB", ["AAPL", "MSFT", "ETH"], ConsoleColor.Magenta, stoppingToken);

        return Task.WhenAll(traderA, traderB);
    }

    private static async Task StartTraderAsync(string name, string[] symbols, ConsoleColor color, CancellationToken ct)
    {
        await using var connection = new HubConnectionBuilder()
            .WithUrl(_hubUrl)
            // .AddMessagePackProtocol() // Ensure the MessagePack NuGet package is referenced
            .WithAutomaticReconnect()
            .Build();

        connection.On<StockPrice>("ReceivePriceUpdate", price =>
        {
            // Lock on a dedicated object, not the Console.Out stream itself
            lock (Console.Out)
            {
                Console.ForegroundColor = color;
                Console.WriteLine($"[{name,-8}] TICK | {price.Symbol,-5} | {price.Price,8:C2}");
                Console.ResetColor();
            }
        });

        connection.On<string>("ReceiveNotification", msg =>
            Console.WriteLine($"[{name,-8}] SYSTEM: {msg}"));

        try
        {
            await connection.StartAsync(ct);

            // Optimization: Parallel subscription requests
            var subTasks = symbols.Select(s => connection.InvokeAsync("SubscribeToStock", s, ct));
            await Task.WhenAll(subTasks);
        }
        catch (OperationCanceledException) { /* Normal shutdown */ }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[{name}] Fatal: {ex.Message}");
        }

        await Task.Delay(Timeout.Infinite, ct);
    }
}
