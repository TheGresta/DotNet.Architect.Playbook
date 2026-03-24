using Playbook.Architecture.CQRS.Application.Common.Interfaces;

namespace Playbook.Architecture.CQRS.Application.Features.Products.Dtos;

/// <summary>
/// Represents a high-level Data Transfer Object (DTO) for conveying product information to external consumers.
/// This record is designed for immutability and follows a flat structure to decouple the internal 
/// domain model from the public API contract.
/// </summary>
/// <param name="Id">The unique identifier for the product, satisfying the <see cref="IHasId"/> contract.</param>
/// <param name="Name">The display name of the product.</param>
/// <param name="Price">The current unit price of the product.</param>
/// <param name="Sku">The Stock Keeping Unit (SKU) used for inventory identification.</param>
public record ProductResponse(
    Guid Id,
    string Name,
    decimal Price,
    string Sku) : IHasId;
