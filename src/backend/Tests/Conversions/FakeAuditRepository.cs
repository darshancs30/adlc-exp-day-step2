using OuterloopLabApi.Data;

namespace Tests.Conversions;

public sealed class FakeAuditRepository : IAuditRepository
{
    private readonly List<AuditRecordEntity> _records = new();

    public Task AddAsync(AuditRecordEntity record, CancellationToken cancellationToken)
    {
        _records.Add(record);
        return Task.CompletedTask;
    }

    public Task<AuditRecordEntity?> GetByIdAsync(string auditId, CancellationToken cancellationToken)
    {
        return Task.FromResult(_records.FirstOrDefault(x => x.Id == auditId));
    }

    public Task<IReadOnlyList<AuditRecordEntity>> ListRecentAsync(int limit, CancellationToken cancellationToken)
    {
        var result = _records
            .OrderByDescending(x => x.ExecutedAtUtcTicks)
            .Take(limit)
            .ToList();
        return Task.FromResult<IReadOnlyList<AuditRecordEntity>>(result);
    }

    public void Seed(params AuditRecordEntity[] records)
    {
        _records.AddRange(records);
    }
}
