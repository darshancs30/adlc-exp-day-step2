using System.Net;
using Microsoft.Azure.Cosmos;

namespace OuterloopLabApi;

public sealed class CosmosAuditRepository : IAuditRepository
{
    private const string PartitionKeyValue = "conversion";
    private readonly Container _container;

    public CosmosAuditRepository(Container container)
    {
        _container = container;
    }

    public async Task AddAsync(Data.AuditRecordEntity record, CancellationToken cancellationToken)
    {
        // Ensure partition key is consistent.
        record.PartitionKey = PartitionKeyValue;
        await _container.CreateItemAsync(record, new PartitionKey(PartitionKeyValue), cancellationToken: cancellationToken);
    }

    public async Task<Data.AuditRecordEntity?> GetByIdAsync(string auditId, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _container.ReadItemAsync<Data.AuditRecordEntity>(auditId, new PartitionKey(PartitionKeyValue), cancellationToken: cancellationToken);
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<Data.AuditRecordEntity>> ListRecentAsync(int limit, CancellationToken cancellationToken)
    {
        var query = new QueryDefinition(
            "SELECT TOP @limit * FROM c WHERE c.pk = @pk ORDER BY c.executedAtUtcTicks DESC")
            .WithParameter("@pk", PartitionKeyValue)
            .WithParameter("@limit", limit);

        var iterator = _container.GetItemQueryIterator<Data.AuditRecordEntity>(query);
        var results = new List<Data.AuditRecordEntity>();
        while (iterator.HasMoreResults)
        {
            var page = await iterator.ReadNextAsync(cancellationToken);
            results.AddRange(page);
        }

        return results;
    }
}
