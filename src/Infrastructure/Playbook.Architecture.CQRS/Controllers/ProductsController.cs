using MediatR;

using Microsoft.AspNetCore.Mvc;

using Playbook.Architecture.CQRS.Application.Features.Products.Commands.Create;
using Playbook.Architecture.CQRS.Application.Features.Products.Queries.Get;

namespace Playbook.Architecture.CQRS.Controllers;

public class ProductsController(ISender mediator) : BaseController(mediator)
{
    [HttpPost]
    public async Task<IActionResult> CreateProduct(
        [FromBody] CreateProductCommand command,
        CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);

        return result.Match(
            product => CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product),
            errors => Problem(errors)
        );
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetProduct(Guid id, CancellationToken ct)
    {
        var query = new GetProductQuery(id);
        var result = await Mediator.Send(query, ct);

        return result.Match(
            Ok,
            errors => Problem(errors)
        );
    }
}
