using Microsoft.AspNetCore.Mvc;
using Playbook.Exceptions.Constants;
using Playbook.Exceptions.Domain;

namespace Playbook.Exceptions.Controllers;

[ApiController]
[Route("api/test-errors")]
public class DiagnosticsController : ControllerBase
{
    // 1. Test 404 - Resource Not Found
    // Expected: INF_NOT_FOUND (Title) + DET_NOT_FOUND (formatted with RES_USER)
    [HttpGet("not-found")]
    public IActionResult GetNotFound()
    {
        throw new NotFoundException(ResourceKeys.User, "12345");
    }

    // 2. Test 400 - Validation Errors
    // Expected: INF_VALIDATION_ERROR (Title) + DET_VALIDATION_SUMMARY (Detail) 
    // + localized errors using VAL_ keys
    [HttpGet("validation")]
    public IActionResult GetValidation()
    {
        var errors = new Dictionary<string, string[]>
        {
            {
                nameof(Email),
                [ValidationKeys.InvalidFormat, ValidationKeys.ProviderBlocked]
            },
            {
                nameof(Password),
                [ValidationKeys.TooShort]
            }
        };

        throw new ValidationException(errors);
    }

    // 3. Test 422 - Business Rule (Dynamic Rule + Args)
    // Expected: INF_BUSINESS_RULE (Title) + RULE_INSUFFICIENT_FUNDS (Detail with Args)
    [HttpGet("business-rule")]
    public IActionResult GetBusinessRule()
    {
        throw new BusinessRuleException(BusinessRuleKeys.InsufficientFunds, 150, "USD");
    }

    // 4. Test 401 - Unauthorized
    // Expected: INF_UNAUTHORIZED (Title) + Handled as LogWarning (not Error)
    [HttpGet("unauthorized")]
    public IActionResult GetUnauthorized()
    {
        // For testing the Mapper, we'd throw a custom UnauthorizedException if created,
        // otherwise, this returns the default ProblemDetails via the Factory.
        return Unauthorized();
    }

    // 5. Test 500 - Standard Exception
    // Expected: INF_INTERNAL_SERVER (Title) + DET_UNEXPECTED_ERROR (Detail)
    [HttpGet("unhandled")]
    public IActionResult GetUnhandled()
    {
        throw new InvalidOperationException("Raw system failure.");
    }

    // 6. Test Safe-Fail (Simulated Crash)
    // Expected: Minimal JSON from HandleSafeFailAsync
    [HttpGet("critical-panic")]
    public IActionResult GetCriticalPanic()
    {
        throw new Exception("Force critical fallback.");
    }

    // Dummy properties for nameof() expressions to avoid magic strings for property names
    private string Email => string.Empty;
    private string Password => string.Empty;
}