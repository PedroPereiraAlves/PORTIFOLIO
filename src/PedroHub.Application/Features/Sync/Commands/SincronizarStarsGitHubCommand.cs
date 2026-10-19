using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PedroHub.Application.Common.Interfaces;
using PedroHub.Domain.Entities;
using PedroHub.Domain.Enums;
using PedroHub.Shared.Results;

namespace PedroHub.Application.Features.Sync.Commands;

public sealed record SincronizarStarsGitHubCommand : IRequest<Result<SincronizarStarsResult>>;

public sealed record SincronizarStarsResult(int Criados, int Atualizados, int TotalRecebidos);

public sealed class SincronizarStarsGitHubCommandHandler
    : IRequestHandler<SincronizarStarsGitHubCommand, Result<SincronizarStarsResult>>
{
    public const string OwnerGitHub = "PedroPereiraAlves";

    private readonly IGitHubStarredClient _gitHub;
    private readonly IApplicationDbContext _db;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<SincronizarStarsGitHubCommandHandler> _logger;

    public SincronizarStarsGitHubCommandHandler(
        IGitHubStarredClient gitHub,
        IApplicationDbContext db,
        IUnitOfWork uow,
        ILogger<SincronizarStarsGitHubCommandHandler> logger)
    {
        _gitHub = gitHub;
        _db = db;
        _uow = uow;
        _logger = logger;
    }

    public async Task<Result<SincronizarStarsResult>> Handle(
        SincronizarStarsGitHubCommand request,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<GitHubStarredRepo> starred;
        try
        {
            starred = await _gitHub.ObterReposAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao consultar repositórios com star no GitHub.");
            return Result.Failure<SincronizarStarsResult>("Não foi possível sincronizar com o GitHub.");
        }

        var agora = DateTime.UtcNow;
        var criados = 0;
        var atualizados = 0;

        var existentes = await _db.Projetos.ToListAsync(cancellationToken);

        var porUrl = existentes.ToDictionary(
            p => NormalizarUrl(p.UrlRepositorio),
            StringComparer.OrdinalIgnoreCase);

        foreach (var repo in starred)
        {
            var url = NormalizarUrl(repo.HtmlUrl);
            var eDoOwner = string.Equals(repo.OwnerLogin, OwnerGitHub, StringComparison.OrdinalIgnoreCase);
            var topicos = repo.Topics.Count == 0 ? null : string.Join(",", repo.Topics);

            if (porUrl.TryGetValue(url, out var projeto))
            {
                projeto.Nome = repo.Name;
                if (string.IsNullOrWhiteSpace(projeto.Descricao))
                    projeto.Descricao = repo.Description;
                projeto.Linguagem = repo.Language;
                projeto.Stars = repo.Stars;
                projeto.Topicos = topicos;
                projeto.SincronizadoEm = agora;
                atualizados++;
            }
            else
            {
                var novo = new Projeto
                {
                    Nome = repo.Name,
                    Descricao = repo.Description,
                    UrlRepositorio = url,
                    Linguagem = repo.Language,
                    Stars = repo.Stars,
                    Topicos = topicos,
                    Visivel = eDoOwner,
                    Destaque = false,
                    Ordem = 0,
                    Origem = OrigemProjeto.GitHub,
                    SincronizadoEm = agora
                };

                _db.Projetos.Add(novo);
                porUrl[url] = novo;
                criados++;
            }
        }

        await _uow.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Sincronização GitHub concluída. Recebidos={Total}, Criados={Criados}, Atualizados={Atualizados}",
            starred.Count,
            criados,
            atualizados);

        return Result.Success(new SincronizarStarsResult(criados, atualizados, starred.Count));
    }

    internal static string NormalizarUrl(string url) => url.Trim().TrimEnd('/');
}
