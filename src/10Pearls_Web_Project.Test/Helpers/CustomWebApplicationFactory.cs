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
    /// Custom WebApplicationFactory for bootstrapping integration tests.
    /// Overrides configuration to use in-memory database and test JWT parameters.
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
            // Override configuration settings for JWT token verification in integration tests
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "Jwt:Key", "SuperSecretKeyLongEnoughToMatchRequirements123!" },
                    { "Jwt:Issuer", "TestIssuer" },
                    { "Jwt:Audience", "TestAudience" },
                    { "Jwt:DurationInMinutes", "60" }
                });
            });

            // Override DbContext registration to use InMemory provider
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<ApplicationDBContext>));

                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<ApplicationDBContext>(options =>
                {
                    options.UseInMemoryDatabase(_databaseName);
                });
            });
        }
    }
}
