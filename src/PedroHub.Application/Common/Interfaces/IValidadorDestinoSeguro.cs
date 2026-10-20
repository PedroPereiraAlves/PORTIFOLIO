namespace PedroHub.Application.Common.Interfaces;

public interface IValidadorDestinoSeguro
{
    Task<ResultadoValidacaoDestino> ValidarAsync(string url, CancellationToken cancellationToken = default);
}

public sealed record ResultadoValidacaoDestino(
    bool EhSeguro,
    string? Motivo,
    Uri? UriNormalizada,
    IReadOnlyList<System.Net.IPAddress>? IpsPermitidos);
