using OuterloopLabApi.Data;

namespace OuterloopLabApi;

public sealed class CurrencyConversionService
{
    private readonly ICurrencyRateProvider _rateProvider;
    private readonly IAuditRepository _auditRepository;

    public CurrencyConversionService(ICurrencyRateProvider rateProvider, IAuditRepository auditRepository)
    {
        _rateProvider = rateProvider;
        _auditRepository = auditRepository;
    }

    public async Task<Models.ConversionResponseDto> ConvertAsync(decimal amount, string fromCurrency, string toCurrency, CancellationToken cancellationToken)
    {
        var executedAtUtc = DateTime.UtcNow;

        var rateLookup = await _rateProvider.GetRateAsync(fromCurrency, toCurrency, cancellationToken);
        var convertedAmount = amount * rateLookup.Rate;

        var auditId = Guid.NewGuid().ToString("n");
        var entity = new AuditRecordEntity
        {
            Id = auditId,
            Amount = amount,
            FromCurrency = fromCurrency,
            ToCurrency = toCurrency,
            Rate = rateLookup.Rate,
            ConvertedAmount = convertedAmount,
            ProviderDateOrMarker = rateLookup.ProviderDateOrMarker,
            ExecutedAtUtcTicks = executedAtUtc.Ticks
        };

        await _auditRepository.AddAsync(entity, cancellationToken);

        return new Models.ConversionResponseDto
        {
            AuditId = auditId,
            Amount = amount,
            FromCurrency = fromCurrency,
            ToCurrency = toCurrency,
            Rate = entity.Rate,
            ConvertedAmount = entity.ConvertedAmount,
            ProviderDateOrMarker = entity.ProviderDateOrMarker,
            ExecutedAtUtc = new DateTime(entity.ExecutedAtUtcTicks, DateTimeKind.Utc).ToString("O")
        };
    }

    public static Models.ConversionAuditRecordDto ToDto(Models.ConversionAuditRecordDto dto, AuditRecordEntity entity)
    {
        dto.AuditId = entity.Id;
        dto.Amount = entity.Amount;
        dto.FromCurrency = entity.FromCurrency;
        dto.ToCurrency = entity.ToCurrency;
        dto.Rate = entity.Rate;
        dto.ConvertedAmount = entity.ConvertedAmount;
        dto.ProviderDateOrMarker = entity.ProviderDateOrMarker;
        dto.ExecutedAtUtc = new DateTime(entity.ExecutedAtUtcTicks, DateTimeKind.Utc).ToString("O");
        return dto;
    }

    public static Models.ConversionAuditRecordDto ToAuditDto(AuditRecordEntity entity)
        => new Models.ConversionAuditRecordDto
        {
            AuditId = entity.Id,
            Amount = entity.Amount,
            FromCurrency = entity.FromCurrency,
            ToCurrency = entity.ToCurrency,
            Rate = entity.Rate,
            ConvertedAmount = entity.ConvertedAmount,
            ProviderDateOrMarker = entity.ProviderDateOrMarker,
            ExecutedAtUtc = new DateTime(entity.ExecutedAtUtcTicks, DateTimeKind.Utc).ToString("O")
        };
}
