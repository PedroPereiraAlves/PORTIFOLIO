using FluentValidation;
using MediatR;
using PedroHub.Application.Common.Interfaces;
using PedroHub.Application.Features.CursorConsumo.Dtos;
using PedroHub.Shared.Results;

namespace PedroHub.Application.Features.CursorConsumo.Commands;

public sealed record SincronizarUsoCursorApiCommand(int Dias = 30)
    : IRequest<Result<ResultadoImportacaoUsoDto>>;

public sealed class SincronizarUsoCursorApiCommandValidator : AbstractValidator<SincronizarUsoCursorApiCommand>
{
    public SincronizarUsoCursorApiCommandValidator()
    {
        RuleFor(x => x.Dias)
            .InclusiveBetween(1, 90)
            .WithMessage("A sincronização aceita de 1 a 90 dias.");
    }
}

public sealed class SincronizarUsoCursorApiCommandHandler
    : IRequestHandler<SincronizarUsoCursorApiCommand, Result<ResultadoImportacaoUsoDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICursorAdminClient _client;

    public SincronizarUsoCursorApiCommandHandler(IApplicationDbContext db, ICursorAdminClient client)
    {
        _db = db;
        _client = client;
    }

    public async Task<Result<ResultadoImportacaoUsoDto>> Handle(
        SincronizarUsoCursorApiCommand request,
        CancellationToken cancellationToken)
    {
        if (!_client.EstaConfigurado)
        {
            return Result.Failure<ResultadoImportacaoUsoDto>(
                "API do Cursor não configurada. Defina HUB_CURSOR_API_KEY ou importe um CSV.");
        }

        var fim = DateTime.UtcNow;
        var inicio = fim.AddDays(-request.Dias);
        var eventos = new List<Domain.CursorConsumo.EventoUsoNormalizado>();
        var truncado = false;
        var cursor = inicio;

        while (cursor < fim)
        {
            var janelaFim = cursor.AddDays(30);
            if (janelaFim > fim)
                janelaFim = fim;

            var fimInclusivo = janelaFim == fim ? janelaFim : janelaFim.AddMilliseconds(-1);
            var pagina = await _client.BuscarAsync(cursor, fimInclusivo, cancellationToken);
            if (pagina.IsFailure)
                return Result.Failure<ResultadoImportacaoUsoDto>(pagina.Error!);

            eventos.AddRange(pagina.Value.Eventos);
            truncado |= pagina.Value.Truncado;
            cursor = janelaFim;
        }

        var aviso = truncado
            ? "A Admin API limitou a página de eventos. O período pode estar incompleto."
            : null;

        var resultado = await GravadorEventosUsoCursor.GravarAsync(
            _db,
            eventos,
            "api",
            null,
            eventos.Count,
            0,
            aviso,
            cancellationToken);

        return Result.Success(resultado);
    }
}
