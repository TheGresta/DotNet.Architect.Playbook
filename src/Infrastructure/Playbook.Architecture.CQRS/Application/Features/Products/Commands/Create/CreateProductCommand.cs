using ErrorOr;

using Playbook.Architecture.CQRS.Application.Common.Interfaces;
using Playbook.Architecture.CQRS.Application.Features.Products.Dtos;

namespace Playbook.Architecture.CQRS.Application.Features.Products.Commands.Create;

/// <summary>
/// Represents a Command request to initialize the creation of a new Product aggregate within the system.
/// Implements <see cref="ICommand{TResponse}"/> to signify a state-changing operation.
/// </summary>
/// <param name="Name">The display name of the product.</param>
/// <param name="Price">The raw decimal value intended for the <see cref="Price"/> Value Object.</param>
/// <param name="Sku">The raw string value intended for the <see cref="Sku"/> Value Object.</param>
public record CreateProductCommand(
    string Name,
    decimal Price,
    string Sku) : ICommand<ErrorOr<ProductResponse>>;
