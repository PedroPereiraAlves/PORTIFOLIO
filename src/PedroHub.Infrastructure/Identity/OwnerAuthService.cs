using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using PedroHub.Application.Common.Interfaces;

namespace PedroHub.Infrastructure.Identity;

public sealed class OwnerAuthService : IOwnerAuthService
{
    public const string RoleOwner = "Owner";

    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<OwnerAuthService> _logger;

    public OwnerAuthService(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        ILogger<OwnerAuthService> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<(bool Sucesso, string? Erro)> LoginAsync(
        string email,
        string senha,
        bool lembrar,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            _logger.LogWarning("Tentativa de login com e-mail desconhecido.");
            return (false, "Credenciais inválidas.");
        }

        if (!await _userManager.IsInRoleAsync(user, RoleOwner))
        {
            _logger.LogWarning("Tentativa de login sem role Owner. UserId={UserId}", user.Id);
            return (false, "Credenciais inválidas.");
        }

        var result = await _signInManager.PasswordSignInAsync(
            user,
            senha,
            isPersistent: lembrar,
            lockoutOnFailure: true);

        if (result.Succeeded)
            return (true, null);

        if (result.IsLockedOut)
        {
            _logger.LogWarning("Conta Owner bloqueada por tentativas inválidas.");
            return (false, "Conta temporariamente bloqueada. Tente novamente mais tarde.");
        }

        return (false, "Credenciais inválidas.");
    }

    public Task LogoutAsync() => _signInManager.SignOutAsync();
}
