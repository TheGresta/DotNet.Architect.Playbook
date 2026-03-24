using MediatR;

using Microsoft.AspNetCore.Mvc;

using Playbook.Architecture.CQRS.Application.Features.Products.Commands.Create;
using Playbook.Architecture.CQRS.Application.Features.Products.Dtos;
using Playbook.Architecture.CQRS.Application.Features.Products.Queries.Get;

namespace Playbook.Architecture.CQRS.Controllers;

[Route("api/products")]
public class ProductsController(ISender mediator) : ApiController(mediator)
{
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id)
        => await SendAndMatch(new GetProductQuery(id));

    [HttpPost]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(CreateProductCommand command)
        => await SendAndCreate(command, nameof(Get));
}
