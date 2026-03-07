namespace Playbook.Exceptions.Core;

public record ExceptionMappingResult(string Title, string Detail, string Code, int StatusCode, IDictionary<string, string[]>? Extensions = null);
