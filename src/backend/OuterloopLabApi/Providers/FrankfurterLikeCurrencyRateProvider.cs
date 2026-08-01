using System.Text.Json;

namespace OuterloopLabApi;

public sealed class FrankfurterLikeCurrencyRateProvider : ICurrencyRateProvider
{
    private readonly System.Net.Http.HttpClient _httpClient;
    private readonly string _baseUrl;

    public FrankfurterLikeCurrencyRateProvider(System.Net.Http.HttpClient httpClient, string baseUrl)
    {
        _httpClient = httpClient;
        _baseUrl = baseUrl.TrimEnd('/');
    }

    public async Task<Providers.RateLookupResult> GetRateAsync(string fromCurrency, string toCurrency, CancellationToken cancellationToken)
    {
        // Provider schema is not trusted; we normalize flexibly.
        var url = $"{_baseUrl}/latest?amount=1&from={Uri.EscapeDataString(fromCurrency)}&to={Uri.EscapeDataString(toCurrency)}";

        try
        {
            using var res = await _httpClient.GetAsync(url, cancellationToken);
            if (!res.IsSuccessStatusCode)
            {
                throw new CurrencyRateProviderUnavailableException("Upstream provider unavailable.");
            }

            using var stream = await res.Content.ReadAsStreamAsync(cancellationToken);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var root = doc.RootElement;

            var rate = TryExtractRate(root, toCurrency);
            if (rate is null)
            {
                throw new CurrencyRateMappingException("Unable to map upstream rate payload.");
            }

            var marker = TryExtractProviderDateOrMarker(root) ?? string.Empty;

            return new Providers.RateLookupResult
            {
                Rate = rate.Value,
                ProviderDateOrMarker = marker
            };
        }
        catch (CurrencyRateMappingException)
        {
            throw;
        }
        catch (CurrencyRateProviderUnavailableException)
        {
            throw;
        }
        catch (Exception)
        {
            // Domain error, do not leak raw exception details.
            throw new CurrencyRateProviderUnavailableException("Upstream provider unavailable.");
        }
    }

    private static decimal? TryExtractRate(JsonElement root, string toCurrency)
    {
        // Common schema: { rates: { "EUR": 0.92 }, date: "2026-.." }
        if (root.TryGetProperty("rates", out var ratesElement) && ratesElement.ValueKind == JsonValueKind.Object)
        {
            if (ratesElement.TryGetProperty(toCurrency, out var v))
            {
                return TryGetDecimal(v);
            }
        }

        // Alternative schema: { conversion_rates: { ... } }
        if (root.TryGetProperty("conversion_rates", out var conversionRatesElement) && conversionRatesElement.ValueKind == JsonValueKind.Object)
        {
            if (conversionRatesElement.TryGetProperty(toCurrency, out var v))
            {
                return TryGetDecimal(v);
            }
        }

        // Fallback: { rate: 0.92 }
        if (root.TryGetProperty("rate", out var rateElement))
        {
            return TryGetDecimal(rateElement);
        }

        return null;
    }

    private static string? TryExtractProviderDateOrMarker(JsonElement root)
    {
        if (root.TryGetProperty("date", out var dateElement) && dateElement.ValueKind == JsonValueKind.String)
        {
            return dateElement.GetString();
        }
        if (root.TryGetProperty("provider_date", out var providerDateElement) && providerDateElement.ValueKind == JsonValueKind.String)
        {
            return providerDateElement.GetString();
        }

        return null;
    }

    private static decimal? TryGetDecimal(JsonElement element)
    {
        try
        {
            return element.ValueKind switch
            {
                JsonValueKind.Number => element.GetDecimal(),
                JsonValueKind.String => decimal.TryParse(element.GetString(), out var d) ? d : null,
                _ => null
            };
        }
        catch
        {
            return null;
        }
    }
}
