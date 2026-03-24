using ErrorOr;

namespace Playbook.Architecture.CQRS.Domain.ValueObjects;

/// <summary>
/// Represents a Price Value Object. 
/// It ensures that monetary values adhere to domain-specific financial constraints, 
/// preventing invalid states like negative pricing or excessive amounts.
/// </summary>
public record Price
{
    /// <summary>
    /// Gets the underlying decimal value of the price.
    /// </summary>
    public decimal Value { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Price"/> record.
    /// Private constructor ensures the <see cref="Create"/> factory method is the only entry point.
    /// </summary>
    /// <param name="value">The validated decimal amount.</param>
    private Price(decimal value) => Value = value;

    /// <summary>
    /// Factory method to create a <see cref="Price"/> instance while enforcing financial invariants.
    /// Validates that the price is positive and stays within a reasonable upper bound.
    /// </summary>
    /// <param name="value">The raw decimal amount.</param>
    /// <returns>A successful <see cref="Price"/> instance or an <see cref="Error.Validation"/> result.</returns>
    public static ErrorOr<Price> Create(decimal value)
    {
        // Enforce that prices must be strictly positive to be valid for sale.
        if (value <= 0)
            return Error.Validation("Price.Invalid", "Price must be greater than zero.");

        // Define a sanity check upper bound to prevent data entry errors or overflow issues.
        if (value > 100000)
            return Error.Validation("Price.TooHigh", "Price cannot exceed 100,000.");

        return new Price(value);
    }

    /// <summary>
    /// Implicitly converts a <see cref="Price"/> instance to its underlying <see cref="decimal"/>.
    /// Allows for seamless usage in mathematical calculations or data persistence.
    /// </summary>
    /// <param name="price">The Price instance to convert.</param>
    public static implicit operator decimal(Price price) => price.Value;
}
