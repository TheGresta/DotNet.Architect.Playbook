using Microsoft.AspNetCore.SignalR.Client;

using Playbook.Messaging.SignalR.Domain.Models;

namespace Playbook.Messaging.SignalR.Terminal;

/// <summary>
/// A simulated integration test client designed to validate real-time stream integrity and Hub connectivity.
/// Inherits from <see cref="BackgroundService"/> to act as a long-running consumer of market data.
/// </summary>
/// <remarks>
/// This client demonstrates the "Virtual User" pattern, spawning multiple concurrent <see cref="HubConnection"/> 
/// instances to stress-test group subscriptions and message delivery across different stock portfolios.
/// </remarks>
public sealed class ManualTestClient(IHostApplicationLifetime lifetime) : BackgroundService
{
    private const string _hubUrl = "http://localhost:5190/stockHub";

    /// <summary>
    /// Orchestrates the startup of multiple concurrent trader simulations.
    /// </summary>
    /// <param name="stoppingToken">Triggered when the host is shutting down.</param>
    /// <returns>A <see cref="Task"/> that completes when all simulated traders have finished execution.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait for the application to be fully started before connecting
        await Task.Run(lifetime.ApplicationStarted.WaitHandle.WaitOne, stoppingToken);

        // Offloading to the ThreadPool immediately to ensure the host startup sequence isn't blocked 
        // by the synchronous overhead of initializing multiple SignalR connections.
        var traderA = StartTraderAsync("ALICE", ["BTC", "ETH", "TSLA"], ConsoleColor.Cyan, stoppingToken);
        var traderB = StartTraderAsync("BOB", ["AAPL", "MSFT", "ETH"], ConsoleColor.Magenta, stoppingToken);

        await Task.WhenAll(traderA, traderB);
    }

    /// <summary>
    /// Initializes a single SignalR connection, configures event handlers, and maintains a subscription state.
    /// </summary>
    /// <param name="name">The display name for the simulated trader.</param>
    /// <param name="symbols">A collection of ticker symbols this client will subscribe to.</param>
    /// <param name="color">The console output color for visual differentiation between clients.</param>
    /// <param name="ct">Cancellation token for graceful termination.</param>
    private static async Task StartTraderAsync(string name, string[] symbols, ConsoleColor color, CancellationToken ct)
    {
        // Utilizing WithAutomaticReconnect to handle transient network failures—critical for financial terminals.
        await using var connection = new HubConnectionBuilder()
            .WithUrl(_hubUrl)
            // Strategy: MessagePack is recommended for high-frequency binary serialization to reduce payload size.
            .AddMessagePackProtocol()
            .WithAutomaticReconnect()
            .Build();

        // Strong typing is achieved by mapping the 'ReceivePriceUpdate' event to the local StockPrice model.
        connection.On<StockPrice>("ReceivePriceUpdate", price =>
        {
            // Thread-safety: Locking on Console.Out to prevent interleaved characters during high-velocity updates.
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

            // Parallel Fan-out: Dispatching all subscription requests simultaneously rather than sequentially 
            // to minimize the "time-to-first-tick" for the client's portfolio.
            var subTasks = symbols.Select(s => connection.InvokeAsync("SubscribeToStock", s, ct));
            await Task.WhenAll(subTasks);
        }
        catch (OperationCanceledException)
        {
            /* Expected behavior during application shutdown */
        }
        catch (Exception ex)
        {
            // High-visibility error reporting for infrastructure-level failures.
            Console.Error.WriteLine($"[{name}] Fatal: {ex.Message}");
            return; // Exit early - no point maintaining a failed connection
        }

        // Maintain the connection until the host signals a shutdown.
        try
        {
            await Task.Delay(Timeout.Infinite, ct);
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown
        }
    }
}
