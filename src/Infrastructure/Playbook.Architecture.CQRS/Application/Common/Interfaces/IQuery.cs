using ErrorOr;

using MediatR;

namespace Playbook.Architecture.CQRS.Application.Common.Interfaces;

// Queries: Read-only, Side-effect free, Cachable
public interface IQuery<TResponse> : IRequest<TResponse> where TResponse : IErrorOr { }
