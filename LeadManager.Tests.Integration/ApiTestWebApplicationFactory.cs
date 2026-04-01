using LeadManager.Application.Abstractions;
using LeadManager.Infrastructure.Caching;
using LeadManager.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LeadManager.Tests.Integration;

public sealed class ApiTestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            var settings = new Dictionary<string, string?>
            {
                ["ConnectionStrings:LeadManagerDatabase"] = "Host=localhost;Port=5432;Database=lead_manager_test;Username=postgres;Password=postgres",
                ["ConnectionStrings:Redis"] = "localhost:6379",
                ["Jwt:Issuer"] = "lead-manager-api-test",
                ["Jwt:Audience"] = "lead-manager-client-test",
                ["Jwt:SigningKey"] = "lead-manager-test-signing-key-123456789",
                ["Jwt:ExpiresInMinutes"] = "60",
                ["Auth:Users:0:Username"] = "admin",
                ["Auth:Users:0:Password"] = "admin123!",
                ["Auth:Users:0:Role"] = "admin",
                ["Auth:Users:1:Username"] = "marketing",
                ["Auth:Users:1:Password"] = "marketing123!",
                ["Auth:Users:1:Role"] = "marketing",
                ["Auth:Users:2:Username"] = "vendas",
                ["Auth:Users:2:Password"] = "vendas123!",
                ["Auth:Users:2:Role"] = "vendas"
            };

            configBuilder.AddInMemoryCollection(settings);
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(ILeadRepository));
            services.RemoveAll(typeof(ILeadHistoryRepository));
            services.RemoveAll(typeof(IRoundRobinStateRepository));
            services.RemoveAll(typeof(ILeadListCache));
            services.RemoveAll(typeof(IUserRepository));
            services.RemoveAll(typeof(IUserPasswordRepository));
            services.RemoveAll(typeof(IRefreshTokenRepository));
            services.RemoveAll(typeof(IOutboxRepository));
            services.RemoveAll(typeof(IAuditTrailRepository));
            services.RemoveAll(typeof(ILeadScoringService));
            services.RemoveAll(typeof(IAssignmentRepository));
            services.AddSingleton<ILeadRepository, InMemoryLeadRepository>();
            services.AddSingleton<ILeadHistoryRepository, InMemoryLeadHistoryRepository>();
            services.AddSingleton<IRoundRobinStateRepository, InMemoryRoundRobinStateRepository>();
            services.AddSingleton<ILeadListCache, RedisLeadListCache>();
            services.AddSingleton<InMemoryUserRepository>();
            services.AddSingleton<IUserRepository>(provider => provider.GetRequiredService<InMemoryUserRepository>());
            services.AddSingleton<IUserPasswordRepository>(provider => provider.GetRequiredService<InMemoryUserRepository>());
            services.AddSingleton<IRefreshTokenRepository, InMemoryRefreshTokenRepository>();
            services.AddSingleton<IOutboxRepository, NoOpOutboxRepository>();
            services.AddSingleton<IAuditTrailRepository, NoOpAuditTrailRepository>();
            services.AddSingleton<ILeadScoringService, NoOpLeadScoringService>();
            services.AddSingleton<IAssignmentRepository, NoOpAssignmentRepository>();

            services.RemoveAll(typeof(IDistributedCache));
            services.AddDistributedMemoryCache();
        });
    }
}
