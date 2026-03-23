using ErrorOr;

using Playbook.Architecture.CQRS.Domain.Common;

namespace Playbook.Architecture.CQRS.Domain.Entities;

public class Product(Guid id, string name, decimal price, string sku) : Entity(id)
{
    public string Name { get; private set; } = name;
    public decimal Price { get; private set; } = price;
    public string Sku { get; private set; } = sku;

    // Domain logic stays inside the entity
    public ErrorOr<Success> UpdatePrice(decimal newPrice)
    {
        if (newPrice <= 0) return DomainErrors.Product.InvalidPrice;
        Price = newPrice;
        return Result.Success;
    }
}
