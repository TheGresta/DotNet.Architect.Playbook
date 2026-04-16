using ErrorOr;

using MediatR;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Playbook.Security.IdP.Application.Abstractions.Data;
using Playbook.Security.IdP.Application.Abstractions.Messaging;
using Playbook.Security.IdP.Application.Abstractions.Security;
using Playbook.Security.IdP.Application.Options;
using Playbook.Security.IdP.Domain.Aggregates.AuditAggregate;

namespace Playbook.Security.IdP.Application.Behaviors;

public sealed partial class AuditBehavior<TRequest, TResponse>(
    IAuditRepository auditRepository,
    IRequestContext requestContext,
    IAuditKeyProvider auditKeyProvider,
    IOptions<AuditOptions> auditOptions,
    ILogger<AuditBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>, IAuditableRequest
    where TResponse : IErrorOr
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // ── 1. Execute the primary business logic first ──────────────────────
        var response = await next();

        // ── 2. Audit is a cross-cutting concern; never break the request ─────
        try
        {
            var auditOptionsValue = auditOptions.Value;

            var hmacKey = await auditKeyProvider.GetKeyAsync(cancellationToken);

            var auditLog = AuditLog.Create(
                actorId: requestContext.UserId,
                action: typeof(TRequest).Name,
                resourceName: request.ResourceName,
                resourceId: request.ResourceId,
                payload: request.GetAuditSummary(),
                ipAddress: requestContext.IpAddress,
                userAgent: requestContext.UserAgent,
                correlationId: requestContext.CorrelationId,
                hmacKey: hmacKey,
                serviceName: auditOptionsValue.ServiceName,
                environment: auditOptionsValue.EnvironmentName,
                isSuccess: !response.IsError);

            await auditRepository.AddAsync(auditLog, cancellationToken);
        }
        catch (Exception ex)
        {
            // ⚠ Audit failure is NEVER allowed to fail a successful request.
            // Log at Critical so on-call is alerted, but return the real response.
            LogAuditFailure(logger, typeof(TRequest).Name, requestContext.CorrelationId, ex);
        }

        return response;
    }

    [LoggerMessage(
        Level = LogLevel.Critical,
        Message = "Audit pipeline failed for request {RequestName} (correlation: {CorrelationId}). " +
                  "The business response was returned but no audit record was persisted.")]
    private static partial void LogAuditFailure(
        ILogger logger,
        string requestName,
        string correlationId,
        Exception ex);
}
