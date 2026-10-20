using PedroHub.Domain.Enums;
using PedroHub.Domain.Monitoramento;

namespace PedroHub.Application.Common.Interfaces;

public interface IChecagemHttpClient
{
    Task<ResultadoChecagemHttp> ExecutarAsync(
        Uri url,
        MetodoChecagem metodo,
        int timeoutMs,
        IReadOnlyList<System.Net.IPAddress> ipsPermitidos,
        CancellationToken cancellationToken = default);
}
