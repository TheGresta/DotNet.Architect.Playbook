using ErrorOr;

using FluentValidation;

using MediatR;

namespace Playbook.Security.IdP.Application.Behaviors;

/// <summary>
/// The primary gatekeeper. Ensures that no request with invalid data 
/// ever reaches a Handler.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : IErrorOr
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var errors = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .Select(failure => Error.Validation(
                code: failure.PropertyName,
                description: failure.ErrorMessage))
            .ToList();

        if (errors.Count == 0)
        {
            return await next();
        }

        return (dynamic)errors;
    }
}
