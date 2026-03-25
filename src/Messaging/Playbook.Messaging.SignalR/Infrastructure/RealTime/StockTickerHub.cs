using Microsoft.AspNetCore.SignalR;

using Playbook.Messaging.SignalR.Domain.Interfaces;

namespace Playbook.Messaging.SignalR.Infrastructure.RealTime;

/// <summary>
/// A high-performance SignalR Hub acting as the real-time gateway for financial ticker subscriptions.
/// Inherits from <see cref="Hub{IStockClient}"/> to provide strongly-typed client communication.
/// </summary>
/// <remarks>
/// This hub leverages SignalR Groups to facilitate efficient broadcasting. When paired with a Redis 
/// backplane, it ensures seamless message distribution across distributed server nodes.
/// </remarks>
public sealed class StockTickerHub(ILogger<StockTickerHub> logger) : Hub<IStockClient>
{
    /// <summary>
    /// Pre-compiled logging delegate using <see cref="LoggerMessage.Define"/> to minimize allocation 
    /// and CPU overhead during high-frequency subscription events.
    /// </summary>
    private static readonly Action<ILogger, string, string, Exception?> _logSubscription =
        LoggerMessage.Define<string, string>(LogLevel.Information, 0, "Client {ConnectionId} subscribed to {Symbol}");

    /// <summary>
    /// Subscribes a client connection to a specific stock symbol's real-time data stream.
    /// </summary>
    /// <param name="symbol">The ticker symbol (e.g., "AAPL") to subscribe to.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous subscription operation.</returns>
    public async ValueTask SubscribeToStock(string symbol)
    {
        // Normalizing the input to ensure group consistency regardless of client casing.
        var cleanSymbol = symbol.ToUpperInvariant();

        // Leveraging SignalR's internal group management. 
        // This is an O(1) operation for the hub, offloading the broadcast complexity to the transport layer.
        await Groups.AddToGroupAsync(Context.ConnectionId, cleanSymbol);

        _logSubscription(logger, Context.ConnectionId, cleanSymbol, null);

        // Immediate feedback to the caller to confirm the subscription state.
        await Clients.Caller.ReceiveNotification($"Subscribed to live feed for: {cleanSymbol}");
    }

    /// <summary>
    /// Removes a client connection from a specific stock symbol's data stream.
    /// </summary>
    /// <param name="symbol">The ticker symbol to unsubscribe from.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous unsubscription operation.</returns>
    public async ValueTask UnsubscribeFromStock(string symbol)
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, symbol.ToUpperInvariant());

    /// <summary>
    /// Lifecycle hook triggered when a new client establishes a persistent connection.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the connection initialization.</returns>
    public override async Task OnConnectedAsync()
    {
        logger.LogInformation("New FinTech Terminal Connected: {Id}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }
}
