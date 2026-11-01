using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PedroHub.Application.Common.Interfaces;
using PedroHub.Application.Common.Options;
using PedroHub.Shared.Results;

namespace PedroHub.Application.Features.Escritorio.Commands;

public sealed record AplicarRetencaoEscritorioCommand : IRequest<Result<int>>;

public sealed class AplicarRetencaoEscritorioCommandHandler
    : IRequestHandler<AplicarRetencaoEscritorioCommand, Result<int>>
{
    private readonly IApplicationDbContext _db;
    private readonly IUnitOfWork _uow;
    private readonly EscritorioOptions _options;
    private readonly ILogger<AplicarRetencaoEscritorioCommandHandler> _logger;

    public AplicarRetencaoEscritorioCommandHandler(
        IApplicationDbContext db,
        IUnitOfWork uow,
        IOptions<EscritorioOptions> options,
        ILogger<AplicarRetencaoEscritorioCommandHandler> logger)
    {
        _db = db;
        _uow = uow;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<Result<int>> Handle(
        AplicarRetencaoEscritorioCommand request,
        CancellationToken cancellationToken)
    {
        var dias = Math.Clamp(_options.RetencaoDias, 1, 3650);
        var corte = DateTime.UtcNow.AddDays(-dias);
        const int lote = 500;
        var total = 0;

        int removidos;
        do
        {
            var velhos = await _db.StatusAgentesHistorico
                .Where(h => h.PublicadoEm < corte)
                .OrderBy(h => h.PublicadoEm)
                .Take(lote)
                .ToListAsync(cancellationToken);

            if (velhos.Count == 0)
                break;

            _db.StatusAgentesHistorico.RemoveRange(velhos);
            await _uow.SaveChangesAsync(cancellationToken);
            removidos = velhos.Count;
            total += removidos;
        } while (removidos == lote);

        do
        {
            var velhos = await _db.EventosAgente
                .Where(e => e.OcorridoEm < corte)
                .OrderBy(e => e.OcorridoEm)
                .Take(lote)
                .ToListAsync(cancellationToken);

            if (velhos.Count == 0)
                break;

            _db.EventosAgente.RemoveRange(velhos);
            await _uow.SaveChangesAsync(cancellationToken);
            removidos = velhos.Count;
            total += removidos;
        } while (removidos == lote);

        if (total > 0)
            _logger.LogInformation("Retenção escritório: removidos={Total} corte={Corte:o}", total, corte);

        return Result.Success(total);
    }
}
