using ErrorOr;

namespace Playbook.Architecture.CQRS.Domain.ValueObjects;

/// <summary>
/// Represents a Stock Keeping Unit (SKU) Value Object. 
/// It encapsulates the business rules and validation logic for product identifiers, 
/// ensuring that only well-formed SKUs exist within the domain layer.
/// </summary>
public record Sku
{
    /// <summary>
    /// Gets the normalized, upper-case string representation of the SKU.
    /// </summary>
    public string Value { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Sku"/> record.
    /// Private constructor enforces the use of the <see cref="Create"/> factory method for instantiation.
    /// </summary>
    /// <param name="value">The validated SKU string.</param>
    private Sku(string value) => Value = value;

    /// <summary>
    /// Factory method to create a <see cref="Sku"/> instance while enforcing domain invariants.
    /// Validates presence and length requirements before returning a normalized instance.
    /// </summary>
    /// <param name="value">The raw string input to be converted into a SKU.</param>
    /// <returns>A successful <see cref="Sku"/> instance or an <see cref="Error.Validation"/> result.</returns>
    public static ErrorOr<Sku> Create(string value)
    {
        // Guard against null, empty, or whitespace-only inputs.
        if (string.IsNullOrWhiteSpace(value))
            return Error.Validation("Sku.Empty", "SKU is required.");

        // Enforce specific length constraints typically required by logistics or database schemas.
        if (value.Length < 5 || value.Length > 15)
            return Error.Validation("Sku.Length", "SKU must be between 5 and 15 characters.");

        // Normalize the string to Upper Invariant to ensure case-insensitive equality and searchability.
        return new Sku(value.ToUpperInvariant());
    }

    /// <summary>
    /// Implicitly converts a <see cref="Sku"/> instance to its underlying <see cref="string"/>.
    /// Facilitates easier integration with infrastructure components like database drivers or DTOs.
    /// </summary>
    /// <param name="sku">The SKU instance to convert.</param>
    public static implicit operator string(Sku sku) => sku.Value;
}
