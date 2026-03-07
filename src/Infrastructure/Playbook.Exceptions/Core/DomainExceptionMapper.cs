using Playbook.Exceptions.Abstraction;
using Playbook.Exceptions.Abstraction.Exceptions;
using Playbook.Exceptions.Constants;

namespace Playbook.Exceptions.Core;

public sealed class DomainExceptionMapper(ILocalizedStringProvider stringProvider) : IExceptionMapper
{
    public ExceptionMappingResult Map(Exception exception)
    {
        if (exception is IMapableException mapable)
        {
            return mapable.Map(this);
        }

        return MapFallback(exception);
    }

    public ExceptionMappingResult MapSpecific(NotFoundException ex) =>
        new(stringProvider.Get(TitleKeys.NotFound),
            stringProvider.Get(DetailKeys.NotFound, stringProvider.Get(ex.ResourceName), ex.Key),
            ex.ErrorCode,
            StatusCodes.Status404NotFound);

    public ExceptionMappingResult MapSpecific(BusinessRuleException ex) =>
        new(stringProvider.Get(TitleKeys.BusinessRule),
            stringProvider.Get(ex.RuleKey, ex.Args),
            ex.RuleKey,
            StatusCodes.Status422UnprocessableEntity);

    public ExceptionMappingResult MapSpecific(ValidationException ex)
    {
        var localizedErrors = new Dictionary<string, string[]>(ex.Errors.Count);
        foreach (var (propertyName, errors) in ex.Errors)
        {
            localizedErrors[propertyName] = Array.ConvertAll(errors, e => stringProvider.Get(e.Message));
        }

        return new(stringProvider.Get(TitleKeys.ValidationError),
            stringProvider.Get(DetailKeys.ValidationSummary),
            ex.ErrorCode,
            StatusCodes.Status400BadRequest,
            localizedErrors);
    }

    public ExceptionMappingResult MapFallback(Exception ex) =>
        new(stringProvider.Get(TitleKeys.BusinessRule),
            stringProvider.Get(ErrorCodes.ActionFailed),
            ErrorCodes.BusinessRuleViolation,
            StatusCodes.Status422UnprocessableEntity);
}
