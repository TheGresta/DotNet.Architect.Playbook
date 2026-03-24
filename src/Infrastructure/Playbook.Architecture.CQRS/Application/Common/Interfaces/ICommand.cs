using ErrorOr;

using MediatR;

namespace Playbook.Architecture.CQRS.Application.Common.Interfaces;

/// <summary>
/// Defines a specialized contract for operations that modify the state of the system.
/// Commands are architecturally intended to be transactional and non-cachable, representing intent to change domain data.
/// </summary>
/// <typeparam name="TResponse">The type of the response, constrained to <see cref="IErrorOr"/> to allow for validation or business rule violations.</typeparam>
public interface ICommand<TResponse> : IRequest<TResponse> where TResponse : IErrorOr { }
