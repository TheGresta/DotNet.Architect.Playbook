namespace Playbook.Security.IdP.Application.Abstractions.Security;

[AttributeUsage(AttributeTargets.Property)]
public sealed class SensitiveDataAttribute : Attribute { }
