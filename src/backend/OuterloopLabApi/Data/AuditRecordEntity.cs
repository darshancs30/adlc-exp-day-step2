using System.Text.Json.Serialization;

namespace OuterloopLabApi.Data;

public sealed class AuditRecordEntity
{
    // Cosmos DB requires "id".
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    // Partition key.
    [JsonPropertyName("pk")]
    public string PartitionKey { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("fromCurrency")]
    public string FromCurrency { get; set; } = string.Empty;

    [JsonPropertyName("toCurrency")]
    public string ToCurrency { get; set; } = string.Empty;

    [JsonPropertyName("rate")]
    public decimal Rate { get; set; }

    [JsonPropertyName("convertedAmount")]
    public decimal ConvertedAmount { get; set; }

    [JsonPropertyName("providerDateOrMarker")]
    public string ProviderDateOrMarker { get; set; } = string.Empty;

    // Ticks for deterministic sub-second reconstruction and ordering.
    [JsonPropertyName("executedAtUtcTicks")]
    public long ExecutedAtUtcTicks { get; set; }
}
