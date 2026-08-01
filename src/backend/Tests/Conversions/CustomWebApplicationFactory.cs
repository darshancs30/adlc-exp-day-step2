using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using OuterloopLabApi;
using OuterloopLabApi.Data;
using OuterloopLabApi.Providers;

namespace Tests.Conversions;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly ICurrencyRateProvider _rateProvider;
    private readonly IAuditRepository _auditRepository;

    public CustomWebApplicationFactory(ICurrencyRateProvider rateProvider, IAuditRepository auditRepository)
    {
        // Must be set before Program.cs builds the service graph.
        Environment.SetEnvironmentVariable("SKIP_COSMOS_PROVISIONING", "true");
        _rateProvider = rateProvider;
        _auditRepository = auditRepository;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Replace services.
            var rateDesc = services.FirstOrDefault(d => d.ServiceType == typeof(ICurrencyRateProvider));
            if (rateDesc != null) services.Remove(rateDesc);
            services.AddSingleton(_rateProvider);

            var repoDesc = services.FirstOrDefault(d => d.ServiceType == typeof(IAuditRepository));
            if (repoDesc != null) services.Remove(repoDesc);
            services.AddSingleton(_auditRepository);
        });
    }
}
