using PedroHub.Domain.Enums;

namespace PedroHub.Application.Common.Interfaces;

public interface IAlertaService
{
    Task NotificarMudancaStatusAsync(
        Guid alvoId,
        string nomeAlvo,
        string url,
        StatusAlvo statusAnterior,
        StatusAlvo statusNovo,
        CancellationToken cancellationToken = default);
}
