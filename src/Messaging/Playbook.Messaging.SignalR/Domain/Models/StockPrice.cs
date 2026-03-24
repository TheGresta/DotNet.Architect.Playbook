using MessagePack;

namespace Playbook.Messaging.SignalR.Domain.Models;

[MessagePackObject]
public readonly record struct StockPrice(
    [property: Key(0)] string Symbol,
    [property: Key(1)] decimal Price,
    [property: Key(2)] double ChangePercent,
    [property: Key(3)] DateTime Timestamp
);
