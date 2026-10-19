using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace PedroHub.Infrastructure.Persistence;

public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    /// <summary>Mesmo UserSecretsId do PedroHub.Web.csproj (design-time sem referência ao Web).</summary>
    private const string WebUserSecretsId = "b3d48655-1fab-4928-bd3b-921430f9ae25";

    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Default");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            var webRoot = ResolverDiretorioWeb();
            var secretsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Microsoft",
                "UserSecrets",
                WebUserSecretsId,
                "secrets.json");

            // Linux/macOS: ~/.microsoft/usersecrets/<id>/secrets.json
            if (!OperatingSystem.IsWindows())
            {
                var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                secretsPath = Path.Combine(home, ".microsoft", "usersecrets", WebUserSecretsId, "secrets.json");
            }

            var configuration = new ConfigurationBuilder()
                .SetBasePath(webRoot)
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddJsonFile(secretsPath, optional: true, reloadOnChange: false)
                .AddEnvironmentVariables()
                .Build();

            connectionString = configuration.GetConnectionString("Default");
        }

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:Default não encontrada. Defina ConnectionStrings__Default no ambiente " +
                "ou grave ConnectionStrings:Default nos user-secrets do PedroHub.Web " +
                "(dotnet user-secrets set \"ConnectionStrings:Default\" \"...\" --project src/PedroHub.Web).");
        }

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new ApplicationDbContext(options);
    }

    private static string ResolverDiretorioWeb()
    {
        var cwd = Directory.GetCurrentDirectory();
        var candidates = new[]
        {
            Path.Combine(cwd, "src", "PedroHub.Web"),
            Path.Combine(cwd, "PedroHub.Web"),
            cwd,
            Path.GetFullPath(Path.Combine(cwd, "..", "PedroHub.Web")),
            Path.GetFullPath(Path.Combine(cwd, "..", "..", "PedroHub.Web")),
            Path.GetFullPath(Path.Combine(
                AppContext.BaseDirectory,
                "..", "..", "..", "..", "PedroHub.Web")),
        };

        foreach (var candidate in candidates)
        {
            if (File.Exists(Path.Combine(candidate, "PedroHub.Web.csproj")))
                return Path.GetFullPath(candidate);
        }

        throw new InvalidOperationException(
            "Não foi possível localizar o projeto PedroHub.Web para carregar appsettings/user-secrets. " +
            "Execute o comando a partir da raiz do repositório com --startup-project src/PedroHub.Web.");
    }
}
