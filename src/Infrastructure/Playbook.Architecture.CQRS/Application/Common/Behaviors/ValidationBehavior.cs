using System.Reflection;

using ErrorOr;

using FluentValidation;

using MediatR;

namespace Playbook.Architecture.CQRS.Application.Common.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>>? validators = null)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : IErrorOr
{
    private static readonly Func<List<Error>, TResponse> _errorFactory = CreateFactory();

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (validators is null || !validators.Any())
        {
            return await next(cancellationToken);
        }

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var errors = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .Select(f => Error.Validation(f.PropertyName, f.ErrorMessage))
            .ToList();

        if (errors.Count == 0)
        {
            return await next(cancellationToken);
        }

        return _errorFactory(errors);
    }

    private static Func<List<Error>, TResponse> CreateFactory()
    {
        var method = typeof(TResponse).GetMethod("From", BindingFlags.Public | BindingFlags.Static, [typeof(List<Error>)])
            ?? throw new InvalidOperationException($"{typeof(TResponse).Name} must implement static From(List<Error>).");

        return (List<Error> errors) => (TResponse)method.Invoke(null, [errors])!;
    }
}
