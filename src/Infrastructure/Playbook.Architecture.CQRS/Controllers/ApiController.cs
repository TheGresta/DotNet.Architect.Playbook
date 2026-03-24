using ErrorOr;

using MediatR;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

using Playbook.Architecture.CQRS.Application.Common.Interfaces;

namespace Playbook.Architecture.CQRS.Controllers;

[ApiController]
public abstract class ApiController(ISender mediator) : ControllerBase
{
    protected readonly ISender Mediator = mediator;

    // Use this for GET / UPDATE
    protected async Task<IActionResult> SendAndMatch<TResponse>(
        IRequest<ErrorOr<TResponse>> request)
    {
        var result = await Mediator.Send(request);
        return result.Match(value => Ok(value), Problem);
    }

    // Update the Base Controller
    protected async Task<IActionResult> SendAndCreate<TResponse>(
    IRequest<ErrorOr<TResponse>> request,
    string actionName) where TResponse : IHasId
    {
        var result = await Mediator.Send(request);

        return result.Match(
            value => CreatedAtAction(actionName, new { id = value.Id }, value),
            Problem);
    }

    /// <summary>
    /// Maps a list of ErrorOr errors to a standardized Problem Details response.
    /// </summary>
    protected IActionResult Problem(List<Error> errors)
    {
        if (errors.Count == 0)
        {
            return Problem();
        }

        // If all errors are validation errors, return a Validation Problem (400)
        if (errors.All(error => error.Type == ErrorType.Validation))
        {
            return ValidationProblem(errors);
        }

        // Otherwise, map the first error to the correct HTTP status code
        return Problem(errors[0]);
    }

    private IActionResult Problem(Error error)
    {
        var statusCode = error.Type switch
        {
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status500InternalServerError,
        };

        return Problem(statusCode: statusCode, title: error.Description);
    }

    private IActionResult ValidationProblem(List<Error> errors)
    {
        var modelStateDictionary = new ModelStateDictionary();

        foreach (var error in errors)
        {
            modelStateDictionary.AddModelError(
                error.Code,
                error.Description);
        }

        return ValidationProblem(modelStateDictionary);
    }
}
