using OuterloopLabApi.Providers;

namespace Tests.Conversions;

public sealed class FakeCurrencyRateProvider : ICurrencyRateProvider
{
    private readonly RateLookupResult _result;
    private readonly Exception? _exception;

    public FakeCurrencyRateProvider(RateLookupResult result)
    {
        _result = result;
        _exception = null;
    }

    public FakeCurrencyRateProvider(Exception exception)
    {
        _exception = exception;
        _result = new RateLookupResult();
    }

    public Task<RateLookupResult> GetRateAsync(string fromCurrency, string toCurrency, CancellationToken cancellationToken)
    {
        if (_exception != null) throw _exception;
        return Task.FromResult(_result);
    }
}
