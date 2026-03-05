using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.Localization;
using Playbook.Exceptions.Mapping;
using Playbook.Exceptions.Resources.Resources;

namespace Playbook.Exceptions;
public sealed class GlobalExceptionHandler(
    IEnumerable<IExceptionMapper> mappers,
    IStringLocalizer<SharedResources> localizer,
    ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);

        // Find a mapper that supports this exception, or fallback to default
        var mapper = mappers.FirstOrDefault(x => x.CanMap(exception));

        var details = mapper?.Map(exception) ?? GetDefaultDetails();

        var response = new ApiErrorResponse
        {
            Status = details.StatusCode,
            Title = details.Title,
            Detail = details.Detail,
            ErrorCode = details.ErrorCode,
            Errors = details.ValidationErrors,
            Instance = httpContext.Request.Path
        };

        httpContext.Response.StatusCode = details.StatusCode;
        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);

        return true;
    }

    private ExceptionMappingResult GetDefaultDetails() => new(
        StatusCodes.Status500InternalServerError,
        localizer["INTERNAL_SERVER_ERROR"],
        localizer["INTERNAL_SERVER_ERROR"],
        "INTERNAL_SERVER_ERROR",
        null);
}