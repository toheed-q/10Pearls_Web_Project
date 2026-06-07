using System;
using System.Collections.Generic;
using System.Linq;
using _10Pearls_Web_Project.Server.DBContext;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace _10Pearls_Web_Project.Test.Helpers
{
    /// <summary>
    /// Custom WebApplicationFactory for integration tests.
    /// Replaces the SQL Server DbContext with an isolated InMemory database per test run
    /// and injects test-safe JWT settings.
    /// </summary>
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string _databaseName;

        public CustomWebApplicationFactory()
        {
            _databaseName = "IntegrationTestDb_" + Guid.NewGuid();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "Jwt:Key",               "SuperSecretKeyLongEnoughToMatchRequirements123!" },
                    { "Jwt:Issuer",            "TestIssuer" },
                    { "Jwt:Audience",          "TestAudience" },
                    { "Jwt:DurationInMinutes", "60" }
                });
            });

            builder.ConfigureServices(services =>
            {
                // Remove all EF Core service descriptors to fully replace the SQL Server
                // provider with InMemory and avoid the "two providers" startup exception.
                // We must remove the internals too because EF Core caches its internal
                // service provider per-options-hash; leaving SQL-Server entries causes
                // a conflict when the InMemory provider is subsequently registered.
                var toRemove = services
                    .Where(d =>
                        d.ServiceType == typeof(DbContextOptions<ApplicationDBContext>) ||
                        d.ServiceType == typeof(DbContextOptions)                       ||
                        d.ServiceType == typeof(ApplicationDBContext)                   ||
                        (d.ServiceType.FullName?.StartsWith("Microsoft.EntityFrameworkCore") == true &&
                         d.ServiceType.FullName?.Contains("Internal")                  == true))
                    .ToList();

                foreach (var d in toRemove)
                    services.Remove(d);

                services.AddDbContext<ApplicationDBContext>(options =>
                    options.UseInMemoryDatabase(_databaseName));
            });
        }
    }
}
