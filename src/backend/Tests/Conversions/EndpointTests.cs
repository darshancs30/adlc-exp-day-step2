using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using OuterloopLabApi;
using OuterloopLabApi.Models;
using OuterloopLabApi.Providers;
using Xunit;

namespace Tests.Conversions;

public sealed class EndpointTests
{
    [Fact]
    public async Task Returns_400_for_invalid_currency_code()
    {
        var provider = new FakeCurrencyRateProvider(new RateLookupResult { Rate = 0.9m, ProviderDateOrMarker = "2026-08-01" });
        var repo = new FakeAuditRepository();
        using var factory = new CustomWebApplicationFactory(provider, repo);
        var client = factory.CreateClient();

        var req = new CurrencyConversionRequestDto { Amount = 10m, FromCurrency = "usd", ToCurrency = "EUR" };
        var res = await client.PostAsJsonAsync("/api/conversions", req);

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Returns_503_ProblemDetails_when_provider_unavailable()
    {
        var provider = new FakeCurrencyRateProvider(new CurrencyRateProviderUnavailableException("unavailable"));
        var repo = new FakeAuditRepository();
        using var factory = new CustomWebApplicationFactory(provider, repo);
        var client = factory.CreateClient();

        var req = new CurrencyConversionRequestDto { Amount = 10m, FromCurrency = "USD", ToCurrency = "EUR" };
        var res = await client.PostAsJsonAsync("/api/conversions", req);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, res.StatusCode);

        var body = await res.Content.ReadFromJsonAsync<ProblemDetailsBody>();
        Assert.NotNull(body);
        Assert.Equal("Currency rate provider unavailable", body!.Title);
    }

    [Fact]
    public async Task Lists_recent_conversions_newest_first()
    {
        var provider = new FakeCurrencyRateProvider(new RateLookupResult { Rate = 1m, ProviderDateOrMarker = "2026-08-01" });
        var repo = new FakeAuditRepository();

        repo.Seed(
            new OuterloopLabApi.Data.AuditRecordEntity
            {
                Id = "old",
                Amount = 1m,
                FromCurrency = "USD",
                ToCurrency = "EUR",
                Rate = 0.9m,
                ConvertedAmount = 0.9m,
                ProviderDateOrMarker = "2026-08-01",
                ExecutedAtUtcTicks = 10
            },
            new OuterloopLabApi.Data.AuditRecordEntity
            {
                Id = "new",
                Amount = 1m,
                FromCurrency = "USD",
                ToCurrency = "EUR",
                Rate = 1.1m,
                ConvertedAmount = 1.1m,
                ProviderDateOrMarker = "2026-08-01",
                ExecutedAtUtcTicks = 20
            }
        );

        using var factory = new CustomWebApplicationFactory(provider, repo);
        var client = factory.CreateClient();

        var res = await client.GetFromJsonAsync<RecentListBody>("/api/conversions/recent?limit=2");
        Assert.NotNull(res);
        Assert.Equal("new", res!.Records[0].AuditId);
        Assert.Equal("old", res!.Records[1].AuditId);
    }

    private sealed class RecentListBody
    {
        public List<RecentRecordBody> Records { get; set; } = new();
    }

    private sealed class RecentRecordBody
    {
        public string AuditId { get; set; } = string.Empty;
    }

    private sealed class ProblemDetailsBody
    {
        public string? Title { get; set; }
        public string? Detail { get; set; }
        public int? Status { get; set; }
    }
}
