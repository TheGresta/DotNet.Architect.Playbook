using ErrorOr;

using Playbook.Architecture.CQRS.Domain.ValueObjects;

namespace Playbook.Architecture.CQRS.Domain.Entities;

public class Product(Guid id, string name, Price price, Sku sku) : Entity(id)
{
    public string Name { get; private set; } = name;
    public Price Price { get; private set; } = price;
    public Sku Sku { get; private set; } = sku;

    public ErrorOr<Updated> Update(string name, decimal priceValue)
    {
        if (string.IsNullOrEmpty(name))
            return Error.Validation("Product.InvalidName", "Product name is required.");

        // We attempt to create a new Price Value Object
        var priceResult = Price.Create(priceValue);

        if (priceResult.IsError) return priceResult.Errors;

        Name = name;
        Price = priceResult.Value;

        return Result.Updated;
    }
}
