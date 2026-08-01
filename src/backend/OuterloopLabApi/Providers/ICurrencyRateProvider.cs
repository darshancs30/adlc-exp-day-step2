using OuterloopLabApi.Providers;

namespace OuterloopLabApi;

public interface ICurrencyRateProvider
{
    Task<RateLookupResult> GetRateAsync(string fromCurrency, string toCurrency, CancellationToken cancellationToken);
}
