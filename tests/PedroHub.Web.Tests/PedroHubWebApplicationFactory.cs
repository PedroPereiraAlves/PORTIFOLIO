using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using PedroHub.Application.Common.Interfaces;
using PedroHub.Infrastructure.Persistence;

namespace PedroHub.Web.Tests;

public sealed class PedroHubWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = "escritorio-tests-" + Guid.NewGuid().ToString("N");
    private readonly string? _gideonApiKey;

    public PedroHubWebApplicationFactory(string? gideonApiKey = "chave-fake-dev")
    {
        _gideonApiKey = gideonApiKey;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            var settings = new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] =
                    "Host=127.0.0.1;Port=5432;Database=pedrohung_test;Username=test;Password=test",
                ["Database:SkipMigrate"] = "true",
                ["HUB_OWNER_EMAIL"] = "owner-test@example.com",
                ["HUB_OWNER_PASSWORD"] = "OwnerTest1!",
                ["Gideon:ApiKey"] = _gideonApiKey ?? "",
                ["HUB_GIDEON_API_KEY"] = _gideonApiKey ?? "",
            };
            config.AddInMemoryCollection(settings);
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IHostedService>();

            foreach (var d in services
                         .Where(d => d.ServiceType == typeof(IDbContextOptionsConfiguration<ApplicationDbContext>))
                         .ToList())
                services.Remove(d);

            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<ApplicationDbContext>();
            services.RemoveAll<IApplicationDbContext>();
            services.RemoveAll<IUnitOfWork>();

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));
            services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
            services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ApplicationDbContext>());
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.EnsureCreated();
        return host;
    }
}
