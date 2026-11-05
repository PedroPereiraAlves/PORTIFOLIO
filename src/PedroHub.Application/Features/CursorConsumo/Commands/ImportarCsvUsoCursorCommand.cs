using FluentValidation;
using MediatR;
using PedroHub.Application.Common.Interfaces;
using PedroHub.Application.Features.CursorConsumo.Dtos;
using PedroHub.Domain.CursorConsumo;
using PedroHub.Shared.Results;

namespace PedroHub.Application.Features.CursorConsumo.Commands;

public sealed record ImportarCsvUsoCursorCommand(string Conteudo, string? NomeArquivo)
    : IRequest<Result<ResultadoImportacaoUsoDto>>;

public sealed class ImportarCsvUsoCursorCommandValidator : AbstractValidator<ImportarCsvUsoCursorCommand>
{
    public const int MaxCaracteres = 4_000_000;

    public ImportarCsvUsoCursorCommandValidator()
    {
        RuleFor(x => x.Conteudo)
            .NotEmpty()
            .WithMessage("O arquivo CSV está vazio.")
            .MaximumLength(MaxCaracteres)
            .WithMessage("O CSV passa de 4 MB.");
    }
}

public sealed class ImportarCsvUsoCursorCommandHandler
    : IRequestHandler<ImportarCsvUsoCursorCommand, Result<ResultadoImportacaoUsoDto>>
{
    private readonly IApplicationDbContext _db;

    public ImportarCsvUsoCursorCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<ResultadoImportacaoUsoDto>> Handle(
        ImportarCsvUsoCursorCommand request,
        CancellationToken cancellationToken)
    {
        var leitura = LeitorCsvUsoCursor.Ler(request.Conteudo);
        if (!leitura.Sucesso)
            return Result.Failure<ResultadoImportacaoUsoDto>(leitura.Erro!);

        var nome = string.IsNullOrWhiteSpace(request.NomeArquivo)
            ? null
            : request.NomeArquivo.Trim();
        if (nome is { Length: > 260 })
            nome = nome[^260..];

        var aviso = leitura.Avisos.Count == 0 ? null : string.Join(" ", leitura.Avisos);
        var resultado = await GravadorEventosUsoCursor.GravarAsync(
            _db,
            leitura.Eventos,
            "csv",
            nome,
            leitura.LinhasDados,
            leitura.Ignorados,
            aviso,
            cancellationToken);

        return Result.Success(resultado);
    }
}
