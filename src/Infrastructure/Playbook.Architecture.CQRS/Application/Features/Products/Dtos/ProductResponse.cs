namespace Playbook.Architecture.CQRS.Application.Features.Products.Dtos;

public record ProductResponse(Guid Id, string Name, decimal Price, string Sku);
