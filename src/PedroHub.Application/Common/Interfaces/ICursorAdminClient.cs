using PedroHub.Domain.CursorConsumo;
using PedroHub.Shared.Results;

namespace PedroHub.Application.Common.Interfaces;

public interface ICursorAdminClient
{
    bool EstaConfigurado { get; }

    Task<Result<LeituraEventosCursor>> BuscarAsync(
        DateTime inicioUtcInclusive,
        DateTime fimUtcInclusive,
        CancellationToken cancellationToken = default);
}

public sealed record LeituraEventosCursor(
    IReadOnlyList<EventoUsoNormalizado> Eventos,
    bool Truncado);
