using MediatR;
using Microsoft.EntityFrameworkCore;
using PedroHub.Application.Common.Interfaces;
using PedroHub.Application.Features.Monitoramento.Dtos;
using PedroHub.Shared.Results;

namespace PedroHub.Application.Features.Monitoramento.Queries;

public sealed record ObterAlvoPorIdQuery(Guid Id) : IRequest<Result<AlvoMonitoradoDto>>;

public sealed class ObterAlvoPorIdQueryHandler
    : IRequestHandler<ObterAlvoPorIdQuery, Result<AlvoMonitoradoDto>>
{
    private readonly IApplicationDbContext _db;

    public ObterAlvoPorIdQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<AlvoMonitoradoDto>> Handle(ObterAlvoPorIdQuery request, CancellationToken cancellationToken)
    {
        var alvo = await _db.AlvosMonitorados
            .AsNoTracking()
            .Include(a => a.Projeto)
            .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);

        if (alvo is null)
            return Result.Failure<AlvoMonitoradoDto>("Alvo não encontrado.");

        return Result.Success(new AlvoMonitoradoDto(
            alvo.Id,
            alvo.Nome,
            alvo.Url,
            alvo.Metodo,
            alvo.IntervaloSegundos,
            alvo.TimeoutMs,
            alvo.StatusEsperado,
            alvo.LimiarDegradadoMs,
            alvo.Ativo,
            alvo.ProjetoId,
            alvo.Projeto?.Nome,
            alvo.Infra,
            alvo.StatusAtual,
            alvo.FalhasConsecutivas,
            alvo.UltimaChecagemEm,
            alvo.ProximaChecagemEm,
            alvo.UltimaMudancaStatusEm,
            null,
            0, 0, 0, 0, 0));
    }
}
