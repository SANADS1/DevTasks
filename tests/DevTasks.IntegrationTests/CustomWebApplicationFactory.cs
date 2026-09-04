using DevTasks.Infrastructure.Identity;
using DevTasks.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using Xunit;

namespace DevTasks.IntegrationTests
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder("postgres:16")
            .WithDatabase("DevTasksTestDb")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        private readonly RedisContainer _redisContainer = new RedisBuilder("redis:latest")
            .Build();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                // Fully self-contained test configuration -- never relies on appsettings.Development.json,
                // which correctly doesn't exist in CI (or any environment other than the developer's own machine).
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = _postgresContainer.GetConnectionString(),
                    ["ConnectionStrings:Redis"] = _redisContainer.GetConnectionString(),
                    ["Jwt:Key"] = "test-only-signing-key-not-for-production-use-32chars-min",
                    ["Jwt:Issuer"] = "DevTasksAPI-Test",
                    ["Jwt:Audience"] = "DevTasksClient-Test",
                    ["Jwt:ExpiryMinutes"] = "60"
                });
            });
        }

        protected override IHost CreateHost(IHostBuilder builder)
        {
            var host = base.CreateHost(builder);
            using var scope = host.Services.CreateScope();
            RoleSeeder.SeedRolesAsync(scope.ServiceProvider).GetAwaiter().GetResult();
            return host;
        }

        public async Task InitializeAsync()
        {
            await _postgresContainer.StartAsync();
            await _redisContainer.StartAsync();

            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseNpgsql(_postgresContainer.GetConnectionString());
            using var context = new AppDbContext(optionsBuilder.Options);
            await context.Database.MigrateAsync();
        }

        public new async Task DisposeAsync()
        {
            await _postgresContainer.DisposeAsync();
            await _redisContainer.DisposeAsync();
        }
    }
}