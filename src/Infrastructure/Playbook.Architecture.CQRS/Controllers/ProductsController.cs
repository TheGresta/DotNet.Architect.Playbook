using MediatR;

using Microsoft.AspNetCore.Mvc;

using Playbook.Architecture.CQRS.Application.Features.Products.Commands.Create;
using Playbook.Architecture.CQRS.Application.Features.Products.Dtos;
using Playbook.Architecture.CQRS.Application.Features.Products.Queries.Get;

namespace Playbook.Architecture.CQRS.Controllers;

/// <summary>
/// Provides high-level RESTful endpoints for managing Product resources.
/// Inherits from <see cref="ApiController"/> to leverage standardized MediatR dispatching
/// and RFC 7807 Problem Details error mapping.
/// </summary>
/// <param name="mediator">The MediatR sender instance for dispatching queries and commands.</param>
[Route("api/products")]
public class ProductsController(ISender mediator) : ApiController(mediator)
{
    /// <summary>
    /// Retrieves a specific product by its unique identifier.
    /// This endpoint utilizes a cached query path via <see cref="GetProductQuery"/>.
    /// </summary>
    /// <param name="id">The unique <see cref="Guid"/> of the product.</param>
    /// <returns>
    /// A <see cref="ProductResponse"/> if the product exists; 
    /// otherwise, a 404 Not Found problem detail.
    /// </returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id)
        // Dispatches the query through the pipeline where it is validated and potentially served from cache.
        => await SendAndMatch(new GetProductQuery(id));

    /// <summary>
    /// Creates a new product based on the provided command data.
    /// This operation triggers validation and, upon success, ensures relevant cache keys are invalidated.
    /// </summary>
    /// <param name="command">The command containing product details (Name, Price, SKU).</param>
    /// <returns>
    /// A 201 Created response containing the newly generated product, 
    /// including a Location header pointing to the <see cref="Get"/> endpoint.
    /// </returns>
    [HttpPost]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(CreateProductCommand command)
        // Dispatches the command. If successful, it uses 'nameof(Get)' to build the resource URI.
        => await SendAndCreate(command, nameof(Get));
}
