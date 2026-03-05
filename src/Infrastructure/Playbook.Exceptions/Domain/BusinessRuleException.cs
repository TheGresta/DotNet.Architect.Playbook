namespace Playbook.Exceptions.Domain;

public sealed class BusinessRuleException(string message, string errorCode = "BUSINESS_RULE_VIOLATION")
    : DomainException(message, errorCode);
