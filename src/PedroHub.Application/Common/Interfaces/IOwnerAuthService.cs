namespace PedroHub.Application.Common.Interfaces;

public interface IOwnerAuthService
{
    Task<(bool Sucesso, string? Erro)> LoginAsync(string email, string senha, bool lembrar, CancellationToken cancellationToken = default);
    Task LogoutAsync();
}
