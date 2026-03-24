using System.Reflection;

using ErrorOr;

using MediatR;

namespace Playbook.Architecture.CQRS.Application.Common.Behaviors;

public sealed class ExceptionHandlingBehavior<TRequest, TResponse>(
    ILogger<ExceptionHandlingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : IErrorOr
{
    private static readonly MethodOrBuilder _errorResultFactory = CreateFactory();

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
            string requestName = typeof(TRequest).Name;
            logger.LogError(ex, "Unhandled exception occurred for {RequestName}", requestName);

            return _errorResultFactory(Error.Unexpected(
                code: "General.UnhandledException",
                description: "An unexpected error occurred on the server."));
        }
    }

    private delegate TResponse MethodOrBuilder(Error error);

    private static MethodOrBuilder CreateFactory()
    {
        var method = typeof(TResponse)
            .GetMethod("From", BindingFlags.Public | BindingFlags.Static, [typeof(Error)])
            ?? throw new InvalidOperationException($"Type {typeof(TResponse).Name} must implement static From(Error).");

        return (Error error) => (TResponse)method.Invoke(null, [error])!;
    }
}
