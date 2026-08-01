namespace OuterloopLabApi.Providers;

public sealed class RateLookupResult
{
    public decimal Rate { get; set; }
    public string ProviderDateOrMarker { get; set; } = string.Empty;
}
