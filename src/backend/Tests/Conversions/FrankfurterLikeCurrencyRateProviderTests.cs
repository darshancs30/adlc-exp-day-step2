using System.Net;
using System.Net.Http;
using System.Text;
using OuterloopLabApi;
using OuterloopLabApi.Providers;
using Xunit;

namespace Tests.Conversions;

public sealed class FrankfurterLikeCurrencyRateProviderTests
{
    [Fact]
    public async Task Maps_rate_from_rates_property()
    {
        var json = "{\"rates\":{\"EUR\":0.92},\"date\":\"2026-08-01\"}";
        var http = CreateHttpClient(json, HttpStatusCode.OK);

        var provider = new FrankfurterLikeCurrencyRateProvider(http, "https://example.test");
        var result = await provider.GetRateAsync("USD", "EUR", CancellationToken.None);

        Assert.Equal(0.92m, result.Rate);
        Assert.Equal("2026-08-01", result.ProviderDateOrMarker);
    }

    [Fact]
    public async Task Maps_rate_from_conversion_rates_property()
    {
        var json = "{\"conversion_rates\":{\"JPY\":155.5},\"date\":\"2026-08-01\"}";
        var http = CreateHttpClient(json, HttpStatusCode.OK);

        var provider = new FrankfurterLikeCurrencyRateProvider(http, "https://example.test");
        var result = await provider.GetRateAsync("USD", "JPY", CancellationToken.None);

        Assert.Equal(155.5m, result.Rate);
    }

    [Fact]
    public async Task Throws_mapping_exception_when_rate_missing()
    {
        var json = "{\"rates\":{\"GBP\":0.7},\"date\":\"2026-08-01\"}";
        var http = CreateHttpClient(json, HttpStatusCode.OK);

        var provider = new FrankfurterLikeCurrencyRateProvider(http, "https://example.test");
        await Assert.ThrowsAsync<CurrencyRateMappingException>(() => provider.GetRateAsync("USD", "EUR", CancellationToken.None));
    }

    private static HttpClient CreateHttpClient(string body, HttpStatusCode status)
    {
        var handler = new TestMessageHandler(body, status);
        return new HttpClient(handler) { BaseAddress = new Uri("https://example.test") };
    }

    private sealed class TestMessageHandler : HttpMessageHandler
    {
        private readonly string _body;
        private readonly HttpStatusCode _status;

        public TestMessageHandler(string body, HttpStatusCode status)
        {
            _body = body;
            _status = status;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var msg = new HttpResponseMessage(_status)
            {
                Content = new StringContent(_body, Encoding.UTF8, "application/json")
            };
            return Task.FromResult(msg);
        }
    }
}
