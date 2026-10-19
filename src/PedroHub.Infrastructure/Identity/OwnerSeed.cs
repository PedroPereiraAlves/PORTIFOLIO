using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace PedroHub.Infrastructure.Identity;

public static class OwnerSeed
{
    public static async Task EnsureOwnerAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("OwnerSeed");
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        var email = configuration["HUB_OWNER_EMAIL"];
        var password = configuration["HUB_OWNER_PASSWORD"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("HUB_OWNER_EMAIL / HUB_OWNER_PASSWORD não definidos. Seed do Owner ignorado.");
            return;
        }

        if (!await roleManager.RoleExistsAsync(OwnerAuthService.RoleOwner))
            await roleManager.CreateAsync(new IdentityRole(OwnerAuthService.RoleOwner));

        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var create = await userManager.CreateAsync(user, password);
            if (!create.Succeeded)
            {
                logger.LogError(
                    "Falha ao criar Owner: {Erros}",
                    string.Join("; ", create.Errors.Select(e => e.Description)));
                return;
            }

            logger.LogInformation("Owner criado via seed.");
        }

        if (!await userManager.IsInRoleAsync(user, OwnerAuthService.RoleOwner))
            await userManager.AddToRoleAsync(user, OwnerAuthService.RoleOwner);
    }
}
