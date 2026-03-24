using MessagePack;

namespace Playbook.Messaging.SignalR.Domain.Models;

/// <summary>
/// Represents a high-performance, immutable snapshot of a stock's market value at a specific point in time.
/// Optimized for low-latency transmission over SignalR using the MessagePack binary serialization protocol.
/// </summary>
/// <param name="Symbol">The unique ticker identifier (e.g., "AAPL").</param>
/// <param name="Price">The current trading price, represented as <see cref="decimal"/> for financial precision.</param>
/// <param name="ChangePercent">The percentage fluctuation since the last market tick or daily open.</param>
/// <param name="Timestamp">The UTC date and time when the price was generated or captured.</param>
/// <remarks>
/// This <see langword="record struct"/> provides value-based equality and eliminates heap allocations 
/// when passed as a method argument, making it ideal for the high-frequency 100ms update loop.
/// </remarks>
[MessagePackObject]
public readonly record struct StockPrice(
    [property: Key(0)] string Symbol,
    [property: Key(1)] decimal Price,
    [property: Key(2)] double ChangePercent,
    [property: Key(3)] DateTime Timestamp
);
