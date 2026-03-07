using Playbook.Exceptions.Abstraction;
using Playbook.Exceptions.Abstraction.Exceptions;
using Playbook.Exceptions.Constants;

namespace Playbook.Exceptions.Core;

/// <summary>
/// Implementation of <see cref="IExceptionMapper"/> that converts domain exceptions 
/// into localized <see cref="ExceptionMappingResult"/> objects using a visitor-like pattern.
/// </summary>
public sealed class DomainExceptionMapper(ILocalizedStringProvider stringProvider) : IExceptionMapper
{
    /// <summary>
    /// Entry point for mapping an exception. Attempts to use polymorphic dispatch 
    /// if the exception implements <see cref="IMapableException"/>.
    /// </summary>
    /// <param name="exception">The exception to be transformed.</param>
    /// <returns>A localized mapping result.</returns>
    public ExceptionMappingResult Map(Exception exception)
    {
        // Double dispatch: If the exception knows how to map itself, let it choose the specific MapSpecific overload.
        if (exception is IMapableException mapable)
        {
            return mapable.Map(this);
        }

        return MapFallback(exception);
    }

    /// <summary>
    /// Maps a <see cref="NotFoundException"/> by localizing both the resource name and the final detail message.
    /// </summary>
    public ExceptionMappingResult MapSpecific(NotFoundException ex) =>
        new(stringProvider.Get(TitleKeys.NotFound),
            stringProvider.Get(DetailKeys.NotFound, stringProvider.Get(ex.ResourceName), ex.Key),
            ex.ErrorCode,
            StatusCodes.Status404NotFound);

    /// <summary>
    /// Maps a <see cref="BusinessRuleException"/> using the rule key and its associated arguments for localization.
    /// </summary>
    public ExceptionMappingResult MapSpecific(BusinessRuleException ex) =>
        new(stringProvider.Get(TitleKeys.BusinessRule),
            stringProvider.Get(ex.RuleKey, ex.Args),
            ex.RuleKey,
            StatusCodes.Status422UnprocessableEntity);

    /// <summary>
    /// Maps a <see cref="ValidationException"/> by iterating through property-specific errors and localizing each message.
    /// </summary>
    public ExceptionMappingResult MapSpecific(ValidationException ex)
    {
        // Pre-allocate dictionary capacity to minimize re-hashings during population.
        var localizedErrors = new Dictionary<string, string[]>(ex.Errors.Count);

        foreach (var (propertyName, errors) in ex.Errors)
        {
            // Efficiently convert the array of ValidationError records into localized string messages.
            localizedErrors[propertyName] = Array.ConvertAll(errors, e => stringProvider.Get(e.Message));
        }

        return new(stringProvider.Get(TitleKeys.ValidationError),
            stringProvider.Get(DetailKeys.ValidationSummary),
            ex.ErrorCode,
            StatusCodes.Status400BadRequest,
            localizedErrors);
    }

    /// <summary>
    /// Provides a generic fallback for exceptions that do not implement <see cref="IMapableException"/>.
    /// </summary>
    public ExceptionMappingResult MapFallback(Exception ex) =>
        new(stringProvider.Get(TitleKeys.BusinessRule),
            stringProvider.Get(ErrorCodes.ActionFailed),
            ErrorCodes.BusinessRuleViolation,
            StatusCodes.Status422UnprocessableEntity);
}
