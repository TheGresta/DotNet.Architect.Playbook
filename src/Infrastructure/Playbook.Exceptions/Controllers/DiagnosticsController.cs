using Microsoft.AspNetCore.Mvc;
using Playbook.Exceptions.Constants;
using Playbook.Exceptions.Domain;

namespace Playbook.Exceptions.Controllers;

[ApiController]
[Route("api/test-errors")]
public class DiagnosticsController : ControllerBase
{
    // 1. Test 404 - Resource Not Found
    [HttpGet("not-found")]
    public IActionResult GetNotFound()
    {
        // This will be caught and mapped to 404 with localized message
        throw new NotFoundException("User", "12345");
    }

    // 2. Test 400 - Validation Errors
    [HttpGet("validation")]
    public IActionResult GetValidation()
    {
        var errors = new Dictionary<string, string[]>
        {
            { "Email", ["Email is not in a valid format.", "Email provider is blocked."] },
            { "Password", ["Password must be at least 8 characters."] }
        };

        // This will test our IEnumerable to Dictionary mapping and 400 status
        throw new ValidationException(errors);
    }

    // 3. Test 422 - Keyed Business Rule (Localization + Args)
    [HttpGet("business-rule")]
    public IActionResult GetBusinessRule()
    {
        // Testing our "Elite" way: Key + Args for dynamic localization
        // Ensure "INSUFFICIENT_FUNDS" exists in your .resx as:
        // "You need {0} {1} more to complete this."
        throw new BusinessRuleException(BusinessRuleKeys.InsufficientFunds, 150, "USD");
    }

    // 4. Test 500 - Standard Exception (Production vs Development)
    [HttpGet("unhandled")]
    public IActionResult GetUnhandled()
    {
        // In Dev: Should show StackTrace and Message
        // In Prod: Should show generic "INTERNAL_SERVER_ERROR"
        throw new InvalidOperationException("This is a raw .NET exception that occurred deep in the logic.");
    }

    // 5. Test 401 - Unauthorized (Testing Smart Logging & Severity)
    [HttpGet("unauthorized")]
    public IActionResult GetUnauthorized()
    {
        // This should be logged as a "Warning", not an "Error" to prevent alert fatigue
        return Unauthorized();
        // Note: If you want to test the Global Handler's mapping for custom Auth exceptions, 
        // you would throw a custom UnauthorizedException here.
    }

    // 6. Test the Safe-Fail Mechanism
    [HttpGet("critial-panic")]
    public IActionResult GetCriticalPanic()
    {
        // To test the "HandleSafeFailAsync" block, you would need to force 
        // an exception INSIDE the GlobalExceptionHandler itself (e.g., passing a null logger).
        // For now, let's simulate a nested error.
        throw new Exception("Simulated crash that could break the handler.");
    }
}