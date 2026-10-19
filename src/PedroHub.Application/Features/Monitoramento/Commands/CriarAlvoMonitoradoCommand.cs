using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PedroHub.Application.Common.Interfaces;
using PedroHub.Domain.Entities;
using PedroHub.Domain.Enums;
using PedroHub.Domain.Monitoramento;
using PedroHub.Shared.Results;

namespace PedroHub.Application.Features.Monitoramento.Commands;

public sealed record CriarAlvoMonitoradoCommand(
    string Nome,
    string Url,
    MetodoChecagem Metodo,
    int IntervaloSegundos,
    int TimeoutMs,
    string StatusEsperado,
    int LimiarDegradadoMs,
    bool Ativo,
    Guid? ProjetoId,
    string? Infra) : IRequest<Result<Guid>>;

public sealed class CriarAlvoMonitoradoCommandValidator : AbstractValidator<CriarAlvoMonitoradoCommand>
{
    public CriarAlvoMonitoradoCommandValidator()
    {
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Url).NotEmpty().MaximumLength(500);
        RuleFor(x => x.IntervaloSegundos).InclusiveBetween(60, 86_400);
        RuleFor(x => x.TimeoutMs).InclusiveBetween(1000, 30_000);
        RuleFor(x => x.LimiarDegradadoMs).InclusiveBetween(100, 30_000);
        RuleFor(x => x.StatusEsperado)
            .NotEmpty()
            .Must(s => StatusEsperadoParser.TentarParse(s, out _, out _))
            .WithMessage("Status esperado deve ser um código (ex.: 200) ou faixa (ex.: 200-399).");
        RuleFor(x => x.Infra).MaximumLength(100);
        RuleFor(x => x.Metodo).IsInEnum();
    }
}

public sealed class CriarAlvoMonitoradoCommandHandler
    : IRequestHandler<CriarAlvoMonitoradoCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;
    private readonly IUnitOfWork _uow;
    private readonly IValidadorDestinoSeguro _validador;

    public CriarAlvoMonitoradoCommandHandler(
        IApplicationDbContext db,
        IUnitOfWork uow,
        IValidadorDestinoSeguro validador)
    {
        _db = db;
        _uow = uow;
        _validador = validador;
    }

    public async Task<Result<Guid>> Handle(CriarAlvoMonitoradoCommand request, CancellationToken cancellationToken)
    {
        var validacao = await _validador.ValidarAsync(request.Url, cancellationToken);
        if (!validacao.EhSeguro || validacao.UriNormalizada is null)
            return Result.Failure<Guid>(validacao.Motivo ?? "URL de destino inválida ou insegura.");

        if (request.ProjetoId is Guid projetoId)
        {
            var existeProjeto = await _db.Projetos.AnyAsync(p => p.Id == projetoId, cancellationToken);
            if (!existeProjeto)
                return Result.Failure<Guid>("Projeto vinculado não encontrado.");
        }

        var agora = DateTime.UtcNow;
        var alvo = new AlvoMonitorado
        {
            Nome = request.Nome.Trim(),
            Url = validacao.UriNormalizada.AbsoluteUri.TrimEnd('/'),
            Metodo = request.Metodo,
            IntervaloSegundos = request.IntervaloSegundos,
            TimeoutMs = request.TimeoutMs,
            StatusEsperado = request.StatusEsperado.Trim(),
            LimiarDegradadoMs = request.LimiarDegradadoMs,
            Ativo = request.Ativo,
            ProjetoId = request.ProjetoId,
            Infra = string.IsNullOrWhiteSpace(request.Infra) ? null : request.Infra.Trim(),
            ProximaChecagemEm = agora
        };

        _db.AlvosMonitorados.Add(alvo);
        await _uow.SaveChangesAsync(cancellationToken);
        return Result.Success(alvo.Id);
    }
}
