using Playbook.Exceptions.Constants;
using Playbook.Exceptions.Domain;
using Playbook.Exceptions.Localization;

namespace Playbook.Exceptions.Mapping;

public sealed class DomainExceptionMapper : IExceptionMapper
{
    private readonly Dictionary<Type, Func<Exception, ExceptionMappingResult>> _mappings = [];
    private readonly ILocalizedStringProvider _stringProvider;

    public DomainExceptionMapper(ILocalizedStringProvider stringProvider)
    {
        _stringProvider = stringProvider;
        ConfigureMappings();
    }

    private void ConfigureMappings()
    {
        // 1. Resource Not Found (404)
        Register<NotFoundException>(ex => new(
            StatusCodes.Status404NotFound,
            _stringProvider.Get(LocalizationKeys.NotFoundTitle),
            _stringProvider.Get(LocalizationKeys.NotFoundTitle, ex.ResourceName, ex.Key),
            ErrorCodes.NotFound,
            null));

        // 2. Validation Errors (400)
        Register<ValidationException>(ex => new(
            StatusCodes.Status400BadRequest,
            _stringProvider.Get(LocalizationKeys.ValidationErrorTitle),
            _stringProvider.Get(LocalizationKeys.ValidationErrorTitle),
            ErrorCodes.ValidationError,
            ex.Errors));

        // 3. Business Rule Violations (422)
        Register<BusinessRuleException>(ex => new(
            StatusCodes.Status422UnprocessableEntity,
            _stringProvider.Get(LocalizationKeys.BusinessRuleTitle),
             _stringProvider.Get(ex.RuleKey, ex.Args),
            ex.RuleKey,
            null));
    }

    private void Register<TException>(Func<TException, ExceptionMappingResult> mapper)
        where TException : Exception
        => _mappings[typeof(TException)] = ex => mapper((TException)ex);

    public bool CanMap(Exception exception) =>
        exception is DomainException || _mappings.ContainsKey(exception.GetType());

    public ExceptionMappingResult Map(Exception exception)
    {
        if (_mappings.TryGetValue(exception.GetType(), out var mapper))
        {
            return mapper(exception);
        }

        // Catch-all for any DomainException not explicitly registered
        return new(
            StatusCodes.Status422UnprocessableEntity,
            _stringProvider.Get(LocalizationKeys.BusinessRuleTitle),
            exception.Message,
            ErrorCodes.BusinessRuleViolation,
            null);
    }
}