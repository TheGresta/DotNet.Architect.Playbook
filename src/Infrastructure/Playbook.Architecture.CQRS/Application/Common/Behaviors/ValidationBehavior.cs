using ErrorOr;

using FluentValidation;

using MediatR;

namespace Playbook.Architecture.CQRS.Application.Common.Behaviors;

public class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>>? validators = null)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : IErrorOr
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        // 1. Guard against null or empty validator lists quickly
        if (validators is null || !validators.Any())
        {
            return await next(ct);
        }

        // 2. Run all validators in parallel
        var validationResults = await Task.WhenAll(
            validators.Select(v => v.ValidateAsync(request, ct)));

        // 3. Extract errors efficiently
        var errors = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .Select(f => Error.Validation(f.PropertyName, f.ErrorMessage))
            .ToList();

        if (errors.Count == 0)
        {
            return await next(ct);
        }

        // We use the ErrorOr factory to create the response type safely.
        return CreateValidationResult(errors);
    }

    private static TResponse CreateValidationResult(List<Error> errors) =>
        // ErrorOr provides a static method 'From' to create the result from a list of errors.
        // This is much faster and safer than using the 'dynamic' keyword.
        (TResponse)typeof(TResponse)
            .GetMethod("From", [typeof(List<Error>)])!
            .Invoke(null, [errors])!;
}
