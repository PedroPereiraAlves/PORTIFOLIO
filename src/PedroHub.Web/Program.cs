using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using PedroHub.Application;
using PedroHub.Infrastructure;
using PedroHub.Infrastructure.Identity;
using PedroHub.Infrastructure.Persistence;
using PedroHub.Web.Middleware;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File(
            path: "logs/pedrohung-.log",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 14));

    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        // Necessário atrás de Render/proxy: confiar nos headers encaminhados.
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
    });

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);

    builder.Services.AddControllersWithViews();
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();

    var connectionString = builder.Configuration.GetConnectionString("Default")
        ?? throw new InvalidOperationException(
            "ConnectionStrings:Default não configurada. Defina ConnectionStrings__Default.");

    builder.Services.AddHealthChecks()
        .AddNpgSql(connectionString);

    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.AddPolicy("login", httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: ObterIpCliente(httpContext),
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                }));

        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: ObterIpCliente(httpContext),
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 200,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                }));
    });

    builder.Services.AddAuthorization(options =>
    {
        options.FallbackPolicy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();
    });

    var app = builder.Build();

    // Antes de HTTPS redirection, auth e rate limit — IP/proto reais via proxy.
    app.UseForwardedHeaders();

    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();
    }

    await OwnerSeed.EnsureOwnerAsync(app.Services);

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler();
        app.UseHsts();
    }
    else
    {
        app.UseExceptionHandler();
    }

    app.UseSerilogRequestLogging();
    app.UseHttpsRedirection();
    app.UseStaticFiles();

    app.Use(async (context, next) =>
    {
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = "DENY";
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        context.Response.Headers["Content-Security-Policy"] =
            "default-src 'self'; " +
            "img-src 'self' data:; " +
            "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; " +
            "font-src 'self' https://fonts.gstatic.com; " +
            "script-src 'self'; " +
            "connect-src 'self'; " +
            "frame-ancestors 'none'; " +
            "base-uri 'self'; " +
            "form-action 'self'";
        await next();
    });

    app.UseRouting();
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapHealthChecks("/health").AllowAnonymous();

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Aplicação encerrada inesperadamente.");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}

static string ObterIpCliente(HttpContext httpContext) =>
    httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

public partial class Program;
