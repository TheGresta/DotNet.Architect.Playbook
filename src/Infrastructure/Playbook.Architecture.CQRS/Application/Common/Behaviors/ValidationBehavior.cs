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

        var errors = new List<Error>();
        foreach (var validator in validators)
        {
            var result = await validator.ValidateAsync(context, cancellationToken);
            errors.AddRange(
                result.Errors
                    .Where(f => f is not null)
                    .Select(f => Error.Validation(f.PropertyName, f.ErrorMessage)));
        }

            // Execute all registered valida
        if (errors.Count == 0)
        {
            return await next(cancellationToken);
        }

        return (TResponse)(object)errors;
    }

    /// <summary>
    /// Uses Reflection to locate the 'From' static factory method on the response type that accepts a list of errors.
    /// This allows the behavior to remain generic while still producing strongly-typed <see cref="IErrorOr"/> responses.
    /// </summary>
    /// <returns>A compiled function that creates a <typeparamref name="TResponse"/> from a list of <see cref="Error"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the response type does not adhere to the expected factory pattern.</exception>
    private static Func<List<Error>, TResponse> CreateFactory()
    {
        var method = typeof(TResponse)
            .GetMethod("op_Implicit", BindingFlags.Public | BindingFlags.Static, [typeof(List<Error>)])
            ?? throw new InvalidOperationException(
                $"Type {typeof(TResponse).Name} must have an implicit conversion operator from List<Error>. " +
                "Ensure TResponse is ErrorOr<T>.");

        return (List<Error> errors) => (TResponse)method.Invoke(null, [errors])!;
    }
}
