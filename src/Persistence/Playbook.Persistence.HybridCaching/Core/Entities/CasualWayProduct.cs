namespace Playbook.Persistence.HybridCaching.Core.Entities;

/// <summary>
/// Represents a standard Plain Old CLR Object (POCO) version of a product entity.
/// </summary>
/// <remarks>
/// Unlike the <see cref="Product"/> record, this implementation does not include explicit 
/// serialization attributes. It is suitable for general-purpose use-cases or serializers 
/// that rely on reflection-based mapping rather than pre-defined binary contracts.
/// </remarks>
public record CasualWayProduct
{
    /// <summary>
    /// Gets the unique identifier for the casual product.
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    /// Gets the display name of the product. Defaults to an empty string.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets the unit price of the product.
    /// </summary>
    public decimal Price { get; init; }

    /// <summary>
    /// Gets the timestamp when the product was first created. Defaults to <see cref="DateTime.UtcNow"/>.
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the timestamp when the product was last modified. Defaults to <see cref="DateTime.UtcNow"/>.
    /// </summary>
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
}
