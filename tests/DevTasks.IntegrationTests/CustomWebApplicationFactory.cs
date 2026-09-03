using DevTasks.Infrastructure.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Testcontainers.PostgreSql;
using Xunit;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL;

namespace DevTasks.IntegrationTests
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder("postgres:16")
            .WithDatabase("DevTasksTestDb")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                // Override the connection string BEFORE Program.cs's own AddDbContext/AddHangfire
                // calls read it, so both point at the disposable test container automatically --
                // no need to remove or re-add any service registrations.
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = _postgresContainer.GetConnectionString()
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

            var optionsBuilder = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<DevTasks.Infrastructure.Persistence.AppDbContext>();
            optionsBuilder.UseNpgsql(_postgresContainer.GetConnectionString());
            using var context = new DevTasks.Infrastructure.Persistence.AppDbContext(optionsBuilder.Options);
            await context.Database.MigrateAsync();
        }

        public new async Task DisposeAsync()
        {
            await _postgresContainer.DisposeAsync();
        }
    }
}