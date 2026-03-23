using ErrorOr;

using MediatR;

using Playbook.Architecture.CQRS.Application.Features.Products.Dtos;

namespace Playbook.Architecture.CQRS.Application.Features.Products.Commands.Create;

public record CreateProductCommand(
    string Name,
    decimal Price,
    string Sku) : IRequest<ErrorOr<ProductResponse>>;
