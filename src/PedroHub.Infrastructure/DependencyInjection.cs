using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using PedroHub.Application.Common.Interfaces;
using PedroHub.Application.Common.Options;
using PedroHub.Infrastructure.Alertas;
using PedroHub.Infrastructure.BackgroundServices;
using PedroHub.Infrastructure.Escritorio;
using PedroHub.Infrastructure.GitHub;
using PedroHub.Infrastructure.Identity;
using PedroHub.Infrastructure.Monitoramento;
using PedroHub.Infrastructure.Persistence;
using PedroHub.Infrastructure.Persistence.Interceptors;

namespace PedroHub.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddSingleton<AuditableEntityInterceptor>();

        services.Configure<MonitoramentoOptions>(configuration.GetSection(MonitoramentoOptions.SectionName));
        services.Configure<ResendOptions>(configuration.GetSection(ResendOptions.SectionName));
        services.Configure<EscritorioOptions>(configuration.GetSection(EscritorioOptions.SectionName));
        services.Configure<GideonOptions>(opts =>
        {
            configuration.GetSection(GideonOptions.SectionName).Bind(opts);
            if (string.IsNullOrWhiteSpace(opts.ApiKey))
                opts.ApiKey = configuration["HUB_GIDEON_API_KEY"];
        });

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            var connectionString = configuration.GetConnectionString("Default")
                ?? throw new InvalidOperationException(
                    "ConnectionStrings:Default não configurada. Defina via env ConnectionStrings__Default, user-secrets ou appsettings.Development.json.");

            options.UseNpgsql(connectionString);
            options.AddInterceptors(sp.GetRequiredService<AuditableEntityInterceptor>());
        });

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ApplicationDbContext>());

        services
            .AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = "PedroHub.Auth";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = environment.IsDevelopment()
                ? CookieSecurePolicy.SameAsRequest
                : CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.LoginPath = "/Account/Login";
            options.AccessDeniedPath = "/Account/AccessDenied";
            options.SlidingExpiration = true;
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
        });

        services.AddScoped<IOwnerAuthService, OwnerAuthService>();
        services.AddSingleton<IDnsResolver, SystemDnsResolver>();
        services.AddScoped<IValidadorDestinoSeguro, ValidadorDestinoSeguro>();
        services.AddScoped<IChecagemHttpClient, ChecagemHttpClient>();
        services.AddSingleton<IAlertaService, ResendAlertaService>();

        services.AddHttpClient<IGitHubStarredClient, GitHubStarredClient>();
        services.AddHttpClient(ChecagemHttpClient.HttpClientName)
            .ConfigurePrimaryHttpMessageHandler(SsrfSocketsHandlerFactory.Create)
            .ConfigureHttpClient(client =>
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("PedroHub-Monitor/1.0");
                client.Timeout = Timeout.InfiniteTimeSpan;
            });
        services.AddHttpClient("resend");

        services.AddHostedService<SincronizarStarsGitHubBackgroundService>();
        services.AddHostedService<MonitoramentoBackgroundService>();
        services.AddHostedService<EscritorioRetencaoBackgroundService>();

        return services;
    }
}
