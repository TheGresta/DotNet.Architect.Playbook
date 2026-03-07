using Microsoft.AspNetCore.Mvc;

using Playbook.Exceptions.Abstraction.Exceptions;
using Playbook.Exceptions.Constants;
using Playbook.Exceptions.Models;

namespace Playbook.Exceptions.Testing;

[ApiController]
[Route("api/diagnostics")] // Renamed for professional standards
public sealed class DiagnosticsController : ControllerBase
{
    // 1. Test 404 - Resource Not Found
    [HttpGet("not-found")]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public IActionResult GetNotFound() =>
        throw new NotFoundException(ResourceKeys.User, "12345");

    // 2. Test 400 - Validation Errors (Testing Masking & Multi-Error parsing)
    [HttpGet("validation")]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public IActionResult GetValidation()
    {
        // Using Collection Expressions ([]) and Dictionary Initializers
        Dictionary<string, ValidationError[]> errors = new()
        {
            ["Email"] = [new(ValidationKeys.InvalidFormat, "user @gmail.com")],
            ["Password"] = [new(ValidationKeys.TooShort, "123")] // Should be masked in logs
        };

        throw new ValidationException(errors);
    }

    // 3. Test 422 - Business Rule (Dynamic Rule + Formatting Args)
    [HttpGet("business-rule")]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public IActionResult GetBusinessRule() =>
        throw DomainErrors.InsufficientFunds(150, "USD");

    // 4. Test 401 - Framework Native
    [HttpGet("unauthorized")]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public IActionResult GetUnauthorized() => Unauthorized();

    // 5. Test 500 - Unhandled System Exception
    [HttpGet("unhandled")]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public IActionResult GetUnhandled() =>
        throw new InvalidOperationException("Generic system crash for testing.");
}
