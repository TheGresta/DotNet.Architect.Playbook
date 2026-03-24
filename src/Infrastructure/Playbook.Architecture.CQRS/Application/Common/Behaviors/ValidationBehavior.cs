using System.Reflection;

using ErrorOr;

using FluentValidation;

using MediatR;

namespace Playbook.Architecture.CQRS.Application.Common.Behaviors;

/// <summary>
/// A centralized validation pipeline behavior that integrates FluentValidation with the MediatR request lifecycle.
/// It asynchronously executes all registered validators for a given request and short-circuits the pipeline 
/// by returning a collection of structured errors if validation fails.
/// </summary>
/// <typeparam name="TRequest">The type of the request to validate.</typeparam>
/// <typeparam name="TResponse">The type of the response, constrained to <see cref="IErrorOr"/> to support domain-driven error reporting.</typeparam>
/// <param name="validators">An optional collection of <see cref="IValidator{TRequest}"/> instances injected via Dependency Injection.</param>
public sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>>? validators = null)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : IErrorOr
{
    /// <summary>
    /// A cached delegate for the static factory method of <typeparamref name="TResponse"/>.
    /// This optimizes performance by preventing repeated reflection lookups for error object instantiation.
    /// </summary>
    private static readonly Func<List<Error>, TResponse> _errorFactory = CreateFactory();

    /// <summary>
    /// Intercepts the request to perform validation before proceeding to the next behavior or handler.
    /// </summary>
    /// <param name="request">The request instance to be validated.</param>
    /// <param name="next">The delegate for the next action in the pipeline.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A <typeparamref name="TResponse"/> containing either the successful result or a list of validation errors.</returns>
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Optimization: bypass validation logic entirely if no validators are registered for this request type.
        if (validators is null || !validators.Any())
        {
            return await next(cancellationToken);
        }

        var context = new ValidationContext<TRequest>(request);

        // Execute all registered validators in parallel to minimize latency, especially when dealing with complex or external rules.
        var validationResults = await Task.WhenAll(
            validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        // Flatten the results from multiple validators into a single list of domain-specific Error objects.
        var errors = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .Select(f => Error.Validation(f.PropertyName, f.ErrorMessage))
            .ToList();

        if (errors.Count == 0)
        {
            return await next(cancellationToken);
        }

        // Short-circuit: Return the error collection without invoking the actual request handler.
        return _errorFactory(errors);
    }

    /// <summary>
    /// Uses Reflection to locate the 'From' static factory method on the response type that accepts a list of errors.
    /// This allows the behavior to remain generic while still producing strongly-typed <see cref="IErrorOr"/> responses.
    /// </summary>
    /// <returns>A compiled function that creates a <typeparamref name="TResponse"/> from a list of <see cref="Error"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the response type does not adhere to the expected factory pattern.</exception>
    private static Func<List<Error>, TResponse> CreateFactory()
    {
        var method = typeof(TResponse).GetMethod("From", BindingFlags.Public | BindingFlags.Static, [typeof(List<Error>)])
            ?? throw new InvalidOperationException($"{typeof(TResponse).Name} must implement static From(List<Error>).");

        return (List<Error> errors) => (TResponse)method.Invoke(null, [errors])!;
    }
}
