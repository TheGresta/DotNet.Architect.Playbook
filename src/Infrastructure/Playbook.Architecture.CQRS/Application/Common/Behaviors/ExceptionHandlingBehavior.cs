using System.Reflection;

using ErrorOr;

using MediatR;

namespace Playbook.Architecture.CQRS.Application.Common.Behaviors;

/// <summary>
/// A global exception handling middleware for the MediatR pipeline.
/// It intercepts unhandled exceptions, logs the failure with request context, and converts the exception 
/// into a standardized <typeparamref name="TResponse"/> using reflection-based factory patterns.
/// </summary>
/// <typeparam name="TRequest">The type of the request being processed.</typeparam>
/// <typeparam name="TResponse">The type of the response, constrained to <see cref="IErrorOr"/> to facilitate functional error handling.</typeparam>
/// <param name="logger">The logger used to capture stack traces and request metadata upon failure.</param>
public sealed class ExceptionHandlingBehavior<TRequest, TResponse>(
    ILogger<ExceptionHandlingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : IErrorOr
{
    /// <summary>
    /// A cached delegate used to instantiate the error response. 
    /// This avoids the overhead of repeated reflection lookups during the exception path.
    /// </summary>
    private static readonly MethodOrBuilder _errorResultFactory = CreateFactory();

    /// <summary>
    /// Wraps the request execution in a try-catch block to ensure no unhandled exceptions escape the application layer.
    /// </summary>
    /// <param name="request">The incoming request object.</param>
    /// <param name="next">The execution delegate for the next behavior or handler in the pipeline.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A <typeparamref name="TResponse"/> containing either the successful result or a mapped unexpected error.</returns>
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        try
        {
            return await next(cancellationToken);
        }
        catch (Exception ex)
        {
            // Capture the request type name to provide context in the telemetry logs.
            string requestName = typeof(TRequest).Name;
            logger.LogError(ex, "Unhandled exception occurred for {RequestName}", requestName);

            // Dynamically invoke the factory to create a failed TResponse without knowing the concrete type at compile time.
            return _errorResultFactory(Error.Unexpected(
                code: "General.UnhandledException",
                description: "An unexpected error occurred on the server."));
        }
    }

    /// <summary>
    /// Represents a factory method that converts a single <see cref="Error"/> into a <typeparamref name="TResponse"/>.
    /// </summary>
    private delegate TResponse MethodOrBuilder(Error error);

    /// <summary>
    /// Uses Reflection to locate and bind to the 'From' static factory method on the response type.
    /// This is executed once per generic instantiation to optimize performance.
    /// </summary>
    /// <returns>A compiled delegate for rapid instantiation of error responses.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the <typeparamref name="TResponse"/> does not adhere to the required 'From' method signature.</exception>
    private static MethodOrBuilder CreateFactory()
    {
        // Locates the static 'From' method typically found in ErrorOr implementations.
        var method = typeof(TResponse)
            .GetMethod("op_Implicit", BindingFlags.Public | BindingFlags.Static, [typeof(Error)])
            ?? throw new InvalidOperationException(
                $"Type {typeof(TResponse).Name} must have an implicit conversion operator from Error. " +
                "Ensure TResponse is ErrorOr<T>.");

        return (Error error) => (TResponse)method.Invoke(null, [error])!;
    }
}
