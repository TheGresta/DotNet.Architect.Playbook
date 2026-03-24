using ErrorOr;

namespace Playbook.Architecture.CQRS.Domain.ValueObjects;

public record Price
{
    public decimal Value { get; init; }

    private Price(decimal value) => Value = value;

    public static ErrorOr<Price> Create(decimal value)
    {
        if (value <= 0)
            return Error.Validation("Price.Invalid", "Price must be greater than zero.");

        if (value > 100000)
            return Error.Validation("Price.TooHigh", "Price cannot exceed 100,000.");

        return new Price(value);
    }

    public static implicit operator decimal(Price price) => price.Value;
}
