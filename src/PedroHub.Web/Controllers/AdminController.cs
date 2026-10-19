using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PedroHub.Application.Features.Projetos.Commands;
using PedroHub.Application.Features.Projetos.Dtos;
using PedroHub.Application.Features.Projetos.Queries;
using PedroHub.Application.Features.Sync.Commands;
using PedroHub.Infrastructure.Identity;
using PedroHub.Web.Models;

namespace PedroHub.Web.Controllers;

[Authorize(Roles = OwnerAuthService.RoleOwner)]
[Route("admin")]
public class AdminController : Controller
{
    private readonly IMediator _mediator;

    public AdminController(IMediator mediator) => _mediator = mediator;

    [HttpGet("")]
    [HttpGet("projetos")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ListarProjetosAdminQuery(), cancellationToken);
        if (result.IsFailure)
        {
            TempData["Erro"] = result.Error;
            return View(Array.Empty<ProjetoDto>());
        }

        return View(result.Value);
    }

    [HttpGet("projetos/novo")]
    public IActionResult Criar() => View("Form", new ProjetoFormViewModel());

    [HttpPost("projetos/novo")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Criar(ProjetoFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View("Form", model);

        var result = await _mediator.Send(new CriarProjetoManualCommand(
            model.Nome,
            model.Descricao,
            model.UrlRepositorio,
            model.UrlDemo,
            model.Linguagem,
            model.Visivel,
            model.Destaque,
            model.Ordem), cancellationToken);

        if (result.IsFailure)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View("Form", model);
        }

        TempData["Sucesso"] = "Projeto criado com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("projetos/{id:guid}/editar")]
    public async Task<IActionResult> Editar(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ObterProjetoPorIdQuery(id), cancellationToken);
        if (result.IsFailure)
        {
            TempData["Erro"] = result.Error;
            return RedirectToAction(nameof(Index));
        }

        var p = result.Value;
        return View("Form", new ProjetoFormViewModel
        {
            Id = p.Id,
            Nome = p.Nome,
            Descricao = p.Descricao,
            UrlRepositorio = p.UrlRepositorio,
            UrlDemo = p.UrlDemo,
            Linguagem = p.Linguagem,
            Visivel = p.Visivel,
            Destaque = p.Destaque,
            Ordem = p.Ordem
        });
    }

    [HttpPost("projetos/{id:guid}/editar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(Guid id, ProjetoFormViewModel model, CancellationToken cancellationToken)
    {
        model.Id = id;
        if (!ModelState.IsValid)
            return View("Form", model);

        var result = await _mediator.Send(new AtualizarProjetoCommand(
            id,
            model.Descricao,
            model.UrlDemo,
            model.Visivel,
            model.Destaque,
            model.Ordem), cancellationToken);

        if (result.IsFailure)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View("Form", model);
        }

        TempData["Sucesso"] = "Projeto atualizado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("projetos/{id:guid}/visibilidade")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AlternarVisibilidade(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new AlternarVisibilidadeProjetoCommand(id), cancellationToken);
        TempData[result.IsSuccess ? "Sucesso" : "Erro"] = result.IsSuccess
            ? "Visibilidade atualizada."
            : result.Error;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("projetos/{id:guid}/destaque")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AlternarDestaque(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new AlternarDestaqueProjetoCommand(id), cancellationToken);
        TempData[result.IsSuccess ? "Sucesso" : "Erro"] = result.IsSuccess
            ? "Destaque atualizado."
            : result.Error;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("sincronizar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Sincronizar(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new SincronizarStarsGitHubCommand(), cancellationToken);
        if (result.IsFailure)
            TempData["Erro"] = result.Error;
        else
            TempData["Sucesso"] =
                $"Sincronização concluída: {result.Value.Criados} criados, {result.Value.Atualizados} atualizados (de {result.Value.TotalRecebidos}).";

        return RedirectToAction(nameof(Index));
    }

    [HttpGet("saude")]
    public IActionResult Saude() => RedirectToAction("Index", "Monitoramento");

    [HttpGet("cursor")]
    public IActionResult Cursor() => View("EmBreve", "Consumo Cursor");

    [HttpGet("gideon")]
    public IActionResult Gideon() => View("EmBreve", "Gideon");
}
