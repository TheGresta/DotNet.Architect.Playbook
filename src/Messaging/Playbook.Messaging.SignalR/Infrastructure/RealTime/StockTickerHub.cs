using Microsoft.AspNetCore.SignalR;

using Playbook.Messaging.SignalR.Domain.Interfaces;

namespace Playbook.Messaging.SignalR.Infrastructure.RealTime;

public sealed class StockTickerHub(ILogger<StockTickerHub> logger) : Hub<IStockClient>
{
    // High-performance Logging via Source Generators
    private static readonly Action<ILogger, string, string, Exception?> _logSubscription =
        LoggerMessage.Define<string, string>(LogLevel.Information, 0, "Client {ConnectionId} subscribed to {Symbol}");

    public async ValueTask SubscribeToStock(string symbol)
    {
        var cleanSymbol = symbol.ToUpperInvariant();

        // SignalR Groups are perfect for this. Redis Backplane handles the multi-node broadcast.
        await Groups.AddToGroupAsync(Context.ConnectionId, cleanSymbol);

        _logSubscription(logger, Context.ConnectionId, cleanSymbol, null);

        await Clients.Caller.ReceiveNotification($"Subscribed to live feed for: {cleanSymbol}");
    }

    public async ValueTask UnsubscribeFromStock(string symbol)
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, symbol.ToUpperInvariant());

    public override async Task OnConnectedAsync()
    {
        logger.LogInformation("New FinTech Terminal Connected: {Id}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }
}
