using OuterloopLabApi.Data;

namespace OuterloopLabApi;

public interface IAuditRepository
{
    Task AddAsync(AuditRecordEntity record, CancellationToken cancellationToken);
    Task<AuditRecordEntity?> GetByIdAsync(string auditId, CancellationToken cancellationToken);
    Task<IReadOnlyList<AuditRecordEntity>> ListRecentAsync(int limit, CancellationToken cancellationToken);
}
