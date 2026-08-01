using Azure.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OuterloopLabApi;
using OuterloopLabApi.Data;
using OuterloopLabApi.Models;
using OuterloopLabApi.Providers;
using System.Net.Http;

var builder = WebApplication.CreateBuilder(args);

// Required so /problem+json serialization matches RFC 7807.
builder.Services.AddProblemDetails();

builder.Services.AddEndpointsApiExplorer();

// Strictly read runtime provider configuration from environment variables.
var currencyApiBaseUrl = Environment.GetEnvironmentVariable("CURRENCY_API_BASE_URL") ?? "https://frankfurter.dev";

builder.Services.AddHttpClient();
builder.Services.AddSingleton<ICurrencyRateProvider>(sp =>
{
    var clientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var httpClient = clientFactory.CreateClient();
    return new FrankfurterLikeCurrencyRateProvider(httpClient, currencyApiBaseUrl);
});

var skipCosmos = string.Equals(Environment.GetEnvironmentVariable("SKIP_COSMOS_PROVISIONING"), "true", StringComparison.OrdinalIgnoreCase);

IAuditRepository auditRepository;

if (skipCosmos)
{
    auditRepository = new InMemoryAuditRepository();
}
else
{
    var settings = CosmosProvisioning.CosmosSettings.ReadFromEnvironment();
    var (cosmosClient, _, container) = await CosmosProvisioning.ProvisionDataPlaneAsync(settings, CancellationToken.None);
    auditRepository = new CosmosAuditRepository(container);
    builder.Services.AddSingleton(cosmosClient);
}

builder.Services.AddSingleton(auditRepository);
builder.Services.AddSingleton<CurrencyConversionService>();

var app = builder.Build();

app.MapPost("/api/conversions", async (CurrencyConversionRequestDto req, CurrencyConversionService conversionService, CancellationToken ct) =>
{
    var validationError = ValidateRequest(req);
    if (validationError != null)
        return validationError;

    try
    {
        var converted = await conversionService.ConvertAsync(req.Amount, req.FromCurrency, req.ToCurrency, ct);
        return Results.Ok(converted);
    }
    catch (CurrencyRateProviderUnavailableException)
    {
        return Results.Problem(
            title: "Currency rate provider unavailable",
            detail: "The upstream currency-rate provider was unavailable or returned an unmappable response.",
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
    catch (CurrencyRateMappingException)
    {
        return Results.Problem(
            title: "Currency rate provider unavailable",
            detail: "The upstream currency-rate provider was unavailable or returned an unmappable response.",
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});

app.MapGet("/api/conversions/{auditId}", async (string auditId, IAuditRepository repo, CancellationToken ct) =>
{
    var record = await repo.GetByIdAsync(auditId, ct);
    if (record is null)
        return Results.NotFound();

    return Results.Ok(CurrencyConversionService.ToAuditDto(record));
});

app.MapGet("/api/conversions/recent", async (int? limit, IAuditRepository repo, CancellationToken ct) =>
{
    var resolvedLimit = (limit ?? 10);
    if (resolvedLimit < 1 || resolvedLimit > 50)
    {
        return Results.Problem(
            title: "Invalid limit",
            detail: "limit must be between 1 and 50.",
            statusCode: StatusCodes.Status400BadRequest);
    }

    var items = await repo.ListRecentAsync(resolvedLimit, ct);
    return Results.Ok(new { records = items.Select(CurrencyConversionService.ToAuditDto).ToList() });
});

app.Run();

static IResult? ValidateRequest(CurrencyConversionRequestDto req)
{
    if (req.Amount <= 0)
    {
        return Results.Problem(
            title: "Invalid amount",
            detail: "amount must be greater than 0.",
            statusCode: StatusCodes.Status400BadRequest);
    }

    if (!IsValidCurrency(req.FromCurrency))
    {
        return Results.Problem(
            title: "Invalid fromCurrency",
            detail: "fromCurrency must be a 3-letter uppercase currency code.",
            statusCode: StatusCodes.Status400BadRequest);
    }

    if (!IsValidCurrency(req.ToCurrency))
    {
        return Results.Problem(
            title: "Invalid toCurrency",
            detail: "toCurrency must be a 3-letter uppercase currency code.",
            statusCode: StatusCodes.Status400BadRequest);
    }

    if (string.Equals(req.FromCurrency, req.ToCurrency, StringComparison.Ordinal))
    {
        return Results.Problem(
            title: "Invalid conversion",
            detail: "fromCurrency and toCurrency must be different.",
            statusCode: StatusCodes.Status400BadRequest);
    }

    return null;
}

static bool IsValidCurrency(string value)
{
    if (string.IsNullOrWhiteSpace(value)) return false;
    if (value.Length != 3) return false;
    for (var i = 0; i < 3; i++)
    {
        var c = value[i];
        if (c < 'A' || c > 'Z') return false;
    }
    return true;
}

public partial class Program { }
