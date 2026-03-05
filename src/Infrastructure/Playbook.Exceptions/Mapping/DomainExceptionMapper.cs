using Microsoft.Extensions.Localization;
using Playbook.Exceptions.Domain;
using Playbook.Exceptions.Resources.Resources;

namespace Playbook.Exceptions.Mapping;

public sealed class DomainExceptionMapper : IExceptionMapper
{
    private readonly Dictionary<Type, Func<Exception, ExceptionMappingResult>> _mappings = [];
    private readonly IStringLocalizer<SharedResources> _localizer;

    public DomainExceptionMapper(IStringLocalizer<SharedResources> localizer)
    {
        _localizer = localizer;
        ConfigureMappings();
    }

    private void ConfigureMappings()
    {
        // One line per exception type. Clean, scannable, and scalable.
        Register<NotFoundException>(ex => new(
            StatusCodes.Status404NotFound,
            _localizer["RESOURCE_NOT_FOUND"],
            _localizer["RESOURCE_NOT_FOUND", ex.ResourceName, ex.Key],
            ex.ErrorCode,
            null));

        Register<ValidationException>(ex => new(
            StatusCodes.Status400BadRequest,
            _localizer["VALIDATION_ERROR"],
            _localizer["VALIDATION_ERROR"],
            ex.ErrorCode,
            ex.Errors.ToDictionary(k => k.Key, v => v.Value)));
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

        // Fallback for any other DomainException not explicitly registered
        return new(
            StatusCodes.Status422UnprocessableEntity,
            _localizer["BUSINESS_RULE_VIOLATION"],
            exception.Message,
            "BUSINESS_RULE_VIOLATION",
            null);
    }
}