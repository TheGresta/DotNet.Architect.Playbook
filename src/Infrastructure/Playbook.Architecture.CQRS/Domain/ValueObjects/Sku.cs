using ErrorOr;

namespace Playbook.Architecture.CQRS.Domain.ValueObjects;

public record Sku
{
    public string Value { get; init; }

    private Sku(string value) => Value = value;

    public static ErrorOr<Sku> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Error.Validation("Sku.Empty", "SKU is required.");

        if (value.Length < 5 || value.Length > 15)
            return Error.Validation("Sku.Length", "SKU must be between 5 and 15 characters.");

        return new Sku(value.ToUpperInvariant());
    }

    public static implicit operator string(Sku sku) => sku.Value;
}
