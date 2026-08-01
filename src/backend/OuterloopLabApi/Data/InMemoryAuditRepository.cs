using System.Collections.Concurrent;

namespace OuterloopLabApi;

public sealed class InMemoryAuditRepository : IAuditRepository
{
    private readonly ConcurrentDictionary<string, OuterloopLabApi.Data.AuditRecordEntity> _items = new();

    public Task AddAsync(OuterloopLabApi.Data.AuditRecordEntity record, CancellationToken cancellationToken)
    {
        _items[record.Id] = record;
        return Task.CompletedTask;
    }

    public Task<OuterloopLabApi.Data.AuditRecordEntity?> GetByIdAsync(string auditId, CancellationToken cancellationToken)
    {
        _items.TryGetValue(auditId, out var value);
        return Task.FromResult(value);
    }

    public Task<IReadOnlyList<OuterloopLabApi.Data.AuditRecordEntity>> ListRecentAsync(int limit, CancellationToken cancellationToken)
    {
        var result = _items.Values
            .OrderByDescending(x => x.ExecutedAtUtcTicks)
            .Take(limit)
            .ToList();

        return Task.FromResult<IReadOnlyList<OuterloopLabApi.Data.AuditRecordEntity>>(result);
    }
}
