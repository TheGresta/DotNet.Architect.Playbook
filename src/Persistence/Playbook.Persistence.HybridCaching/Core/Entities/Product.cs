using ProtoBuf;

namespace Playbook.Persistence.HybridCaching.Core.Entities;

/// <summary>
/// Represents a high-performance product entity optimized for Protobuf serialization.
/// </summary>
/// <remarks>
/// This record utilizes <see cref="ProtoContractAttribute"/> and <see cref="ProtoMemberAttribute"/> 
/// to ensure deterministic, binary-efficient serialization. It is intended for use in 
/// high-traffic caching scenarios where payload size and CPU cycles are critical.
/// </remarks>
[ProtoContract]
public record Product
{
    /// <summary>
    /// Gets the unique identifier for the product.
    /// </summary>
    [ProtoMember(1)] public int Id { get; init; }

    /// <summary>
    /// Gets the display name of the product. Defaults to an empty string.
    /// </summary>
    [ProtoMember(2)] public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets the unit price of the product.
    /// </summary>
    [ProtoMember(3)] public decimal Price { get; init; }

    /// <summary>
    /// Gets the timestamp when the product was first created. Defaults to <see cref="DateTime.UtcNow"/>.
    /// </summary>
    [ProtoMember(4)] public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the timestamp when the product was last modified. Defaults to <see cref="DateTime.UtcNow"/>.
    /// </summary>
    [ProtoMember(5)] public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
}
