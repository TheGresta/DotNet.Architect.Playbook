using ErrorOr;

using MediatR;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

using Playbook.Architecture.CQRS.Application.Common.Interfaces;

namespace Playbook.Architecture.CQRS.Controllers;

/// <summary>
/// A foundational API controller that abstracts the MediatR communication and provides 
/// standardized mapping between domain results (<see cref="ErrorOr"/>) and HTTP responses.
/// This controller implements the "Clean Architecture" pattern by ensuring the web layer 
/// only interacts with the application layer via the Mediator.
/// </summary>
/// <param name="mediator">The MediatR sender used to dispatch requests to the application layer.</param>
[ApiController]
public abstract class ApiController(ISender mediator) : ControllerBase
{
    /// <summary>
    /// The Mediator instance available to derived controllers for dispatching commands and queries.
    /// </summary>
    protected readonly ISender Mediator = mediator;

    /// <summary>
    /// Dispatches a request and automatically matches the result to an 'OK (200)' response 
    /// or a standardized Problem Details response based on the error type.
    /// Suitable for Read operations (GET) or standard Updates (PUT/PATCH).
    /// </summary>
    /// <typeparam name="TResponse">The type of the successful response data.</typeparam>
    /// <param name="request">The MediatR request to be processed.</param>
    /// <returns>A task representing the <see cref="IActionResult"/> result.</returns>
    protected async Task<IActionResult> SendAndMatch<TResponse>(
        IRequest<ErrorOr<TResponse>> request)
    {
        var result = await Mediator.Send(request, HttpContext.RequestAborted);
        // Uses the ErrorOr Match method to functionally branch between success and error paths.
        return result.Match(value => Ok(value), Problem);
    }

    /// <summary>
    /// Dispatches a creation request and matches the result to a 'Created (201)' response,
    /// including the 'Location' header pointing to the newly created resource.
    /// </summary>
    /// <typeparam name="TResponse">The type of the response, constrained to objects with a unique ID.</typeparam>
    /// <param name="request">The creation command to be processed.</param>
    /// <param name="actionName">The name of the GET action used to retrieve the created resource.</param>
    /// <returns>A task representing the <see cref="IActionResult"/> result.</returns>
    protected async Task<IActionResult> SendAndCreate<TResponse>(
    IRequest<ErrorOr<TResponse>> request,
    string actionName) where TResponse : IHasId
    {
        var result = await Mediator.Send(request, HttpContext.RequestAborted);

        return result.Match(
            // On success, returns 201 Created with the resource URI and the object body.
            value => CreatedAtAction(actionName, new { id = value.Id }, value),
            Problem);
    }

    /// <summary>
    /// Maps a list of domain <see cref="Error"/> objects to a standardized RFC 7807 Problem Details response.
    /// This ensures consistent error reporting across the entire API surface.
    /// </summary>
    /// <param name="errors">The collection of errors returned from the application layer.</param>
    /// <returns>An <see cref="IActionResult"/> containing the mapped problem details.</returns>
    protected IActionResult Problem(List<Error> errors)
    {
        // Fallback for an empty error list to prevent returning a blank success as a problem.
        if (errors.Count == 0)
        {
            return Problem();
        }

        // Optimization: If all errors are validation-related, group them into a 400 Validation Problem.
        if (errors.All(error => error.Type == ErrorType.Validation))
        {
            return ValidationProblem(errors);
        }

        // For mixed errors or non-validation errors, prioritize the first error for the status code.
        return Problem(errors[0]);
    }

    /// <summary>
    /// Maps a single <see cref="Error"/> to the corresponding HTTP status code and title.
    /// </summary>
    private IActionResult Problem(Error error)
    {
        var statusCode = error.Type switch
        {
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status500InternalServerError, // Default to 500 for Unexpected or Failure types.
        };

        return Problem(statusCode: statusCode, title: error.Description);
    }

    /// <summary>
    /// Converts a list of validation errors into a <see cref="ModelStateDictionary"/> 
    /// to leverage the built-in ASP.NET Core validation problem response.
    /// </summary>
    private IActionResult ValidationProblem(List<Error> errors)
    {
        var modelStateDictionary = new ModelStateDictionary();

        foreach (var error in errors)
        {
            // The error code is used as the key (property name), and description as the message.
            modelStateDictionary.AddModelError(
                error.Code,
                error.Description);
        }

        return ValidationProblem(modelStateDictionary);
    }
}
