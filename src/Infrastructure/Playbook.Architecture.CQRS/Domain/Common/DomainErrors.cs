using ErrorOr;

namespace Playbook.Architecture.CQRS.Domain.Common;

public static class DomainErrors
{
    public static class Product
    {
        public static Error NotFound => Error.NotFound(
            code: "Product.NotFound",
            description: "The requested product was not found.");

        public static Error InvalidPrice => Error.Validation(
            code: "Product.InvalidPrice",
            description: "Product price must be greater than zero.");
    }
}
