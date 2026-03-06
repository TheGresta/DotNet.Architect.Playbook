using Playbook.Exceptions.Abstraction;
using Playbook.Exceptions.Abstraction.Exceptions;
using Playbook.Exceptions.Constants;

namespace Playbook.Exceptions.Core;

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
        // 1. Dynamic NotFound (Uses INF_ and DET_ protocols)
        Register<NotFoundException>(ex => new ExceptionMappingResult(
            StatusCodes.Status404NotFound,
            _stringProvider.Get(TitleKeys.NotFound),
            // Nested Translation: First translate the Resource (RES_), 
            // then inject it into the Detail template (DET_).
            _stringProvider.Get(DetailKeys.NotFound,
                _stringProvider.Get(ex.ResourceName), ex.Key),
            ex.ErrorCode,
            null));

        // 2. Dynamic Business Rules (Uses INF_ and RULE_ protocols)
        Register<BusinessRuleException>(ex => new ExceptionMappingResult(
            StatusCodes.Status422UnprocessableEntity,
            _stringProvider.Get(TitleKeys.BusinessRule),
            _stringProvider.Get(ex.RuleKey, ex.Args), // Dynamic RULE_ lookup
            ex.RuleKey, // We use the specific rule key as the error code for the frontend
            null));

        // 3. Dynamic Validation (Uses INF_ and VAL_ protocols)
        Register<ValidationException>(ex =>
        {
            var localizedErrors = new Dictionary<string, string[]>();

            foreach (var error in ex.Errors)
            {
                // error.Key is the PropertyName (e.g., "Email")
                // error.Value is an array of ValidationError objects
                var translatedMessages = error.Value
                    .Select(validationError =>
                    {
                        // We extract the 'Message' (which is our VAL_ key) 
                        // and pass it to the string provider for translation.
                        return _stringProvider.Get(validationError.Message);
                    })
                    .ToArray();

                localizedErrors.Add(error.Key, translatedMessages);
            }

            return new ExceptionMappingResult(
                StatusCodes.Status400BadRequest,
                _stringProvider.Get(TitleKeys.ValidationError),
                _stringProvider.Get(DetailKeys.ValidationSummary),
                ex.ErrorCode,
                localizedErrors);
        });
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
        // Uses the SharedResources fallback via the Smart Router
        return new(
            StatusCodes.Status422UnprocessableEntity,
            _stringProvider.Get(TitleKeys.BusinessRule),
            _stringProvider.Get(ErrorCodes.ActionFailed),
            ErrorCodes.BusinessRuleViolation,
            null);
    }
}