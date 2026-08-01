namespace OuterloopLabApi.Models;

public sealed class ConversionAuditRecordDto
{
    public string AuditId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string FromCurrency { get; set; } = string.Empty;
    public string ToCurrency { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public decimal ConvertedAmount { get; set; }
    public string ProviderDateOrMarker { get; set; } = string.Empty;
    public string ExecutedAtUtc { get; set; } = string.Empty;
}
