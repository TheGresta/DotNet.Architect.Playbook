using ErrorOr;

using MediatR;

namespace Playbook.Architecture.CQRS.Application.Common.Interfaces;

// Commands: State-changing, Transactional, Non-cachable
public interface ICommand<TResponse> : IRequest<TResponse> where TResponse : IErrorOr { }
