using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PedroHub.Application.Common.Interfaces;
using PedroHub.Domain.Enums;
using PedroHub.Domain.Monitoramento;
using PedroHub.Shared.Results;

namespace PedroHub.Application.Features.Monitoramento.Commands;

public sealed record AtualizarAlvoMonitoradoCommand(
    Guid Id,
    string Nome,
    string Url,
    MetodoChecagem Metodo,
    int IntervaloSegundos,
    int TimeoutMs,
    string StatusEsperado,
    int LimiarDegradadoMs,
    bool Ativo,
    Guid? ProjetoId,
    string? Infra) : IRequest<Result>;

public sealed class AtualizarAlvoMonitoradoCommandValidator : AbstractValidator<AtualizarAlvoMonitoradoCommand>
{
    public AtualizarAlvoMonitoradoCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
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

public sealed class AtualizarAlvoMonitoradoCommandHandler
    : IRequestHandler<AtualizarAlvoMonitoradoCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly IUnitOfWork _uow;
    private readonly IValidadorDestinoSeguro _validador;

    public AtualizarAlvoMonitoradoCommandHandler(
        IApplicationDbContext db,
        IUnitOfWork uow,
        IValidadorDestinoSeguro validador)
    {
        _db = db;
        _uow = uow;
        _validador = validador;
    }

    public async Task<Result> Handle(AtualizarAlvoMonitoradoCommand request, CancellationToken cancellationToken)
    {
        var alvo = await _db.AlvosMonitorados.FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);
        if (alvo is null)
            return Result.Failure("Alvo não encontrado.");

        var validacao = await _validador.ValidarAsync(request.Url, cancellationToken);
        if (!validacao.EhSeguro || validacao.UriNormalizada is null)
            return Result.Failure(validacao.Motivo ?? "URL de destino inválida ou insegura.");

        if (request.ProjetoId is Guid projetoId)
        {
            var existeProjeto = await _db.Projetos.AnyAsync(p => p.Id == projetoId, cancellationToken);
            if (!existeProjeto)
                return Result.Failure("Projeto vinculado não encontrado.");
        }

        alvo.Nome = request.Nome.Trim();
        alvo.Url = validacao.UriNormalizada.AbsoluteUri.TrimEnd('/');
        alvo.Metodo = request.Metodo;
        alvo.IntervaloSegundos = request.IntervaloSegundos;
        alvo.TimeoutMs = request.TimeoutMs;
        alvo.StatusEsperado = request.StatusEsperado.Trim();
        alvo.LimiarDegradadoMs = request.LimiarDegradadoMs;
        alvo.Ativo = request.Ativo;
        alvo.ProjetoId = request.ProjetoId;
        alvo.Infra = string.IsNullOrWhiteSpace(request.Infra) ? null : request.Infra.Trim();

        await _uow.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
