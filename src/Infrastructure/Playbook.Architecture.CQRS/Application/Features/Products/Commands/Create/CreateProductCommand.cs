using ErrorOr;

using Playbook.Architecture.CQRS.Application.Common.Interfaces;
using Playbook.Architecture.CQRS.Application.Features.Products.Dtos;

namespace Playbook.Architecture.CQRS.Application.Features.Products.Commands.Create;

public record CreateProductCommand(
    string Name,
    decimal Price,
    string Sku) : ICommand<ErrorOr<ProductResponse>>;
