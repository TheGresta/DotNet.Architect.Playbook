using ErrorOr;

using MediatR;

namespace Playbook.Architecture.CQRS.Application.Common.Behaviors;

public class ExceptionHandlingBehavior<TRequest, TResponse>(
    ILogger<ExceptionHandlingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : IErrorOr
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        try
        {
            return await next(ct);
        }
        catch (Exception ex)
        {
            // 1. Log the full stack trace for internal debugging
            logger.LogError(ex, "Unhandled exception occurred for {RequestName}", typeof(TRequest).Name);

            // 2. Return a generic "Unexpected" error to the user
            var error = Error.Unexpected(
                code: "General.UnhandledException",
                description: "An unexpected error occurred on the server.");

            // 3. Use our static helper to create the ErrorOr response without 'dynamic'
            return CreateErrorResult(error);
        }
    }

    private static TResponse CreateErrorResult(Error error)
    {
        // Big Tech optimization: We convert the single error into a list
        var errors = new List<Error> { error };

        return (TResponse)typeof(TResponse)
            .GetMethod("From", [typeof(List<Error>)])!
            .Invoke(null, [errors])!;
    }
}
