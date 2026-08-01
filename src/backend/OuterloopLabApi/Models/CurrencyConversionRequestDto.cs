namespace OuterloopLabApi.Models;

public sealed class CurrencyConversionRequestDto
{
    public decimal Amount { get; set; }
    public string FromCurrency { get; set; } = string.Empty;
    public string ToCurrency { get; set; } = string.Empty;
}
