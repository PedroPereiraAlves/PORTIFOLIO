using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PedroHub.Application.Features.Escritorio.Commands;
using PedroHub.Application.Features.Escritorio.Queries;
using PedroHub.Domain.Enums;
using PedroHub.Infrastructure.Identity;
using PedroHub.Web.Models;

namespace PedroHub.Web.Controllers;

[Authorize(Roles = OwnerAuthService.RoleOwner)]
[Route("admin/escritorio")]
public sealed class EscritorioController : Controller
{
    private readonly IMediator _mediator;

    public EscritorioController(IMediator mediator) => _mediator = mediator;

    [HttpGet("")]
    public IActionResult Index()
    {
        ViewData["Title"] = "Escritório";
        return View();
    }

    [HttpGet("estado")]
    [Produces("application/json")]
    public async Task<IActionResult> Estado(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ObterEstadoEscritorioQuery(), cancellationToken);
        if (result.IsFailure)
            return BadRequest(new { erro = result.Error });

        return Ok(result.Value);
    }

    [HttpGet("agentes")]
    public async Task<IActionResult> Agentes(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ListarAgentesAdminQuery(), cancellationToken);
        if (result.IsFailure)
        {
            TempData["Erro"] = result.Error;
            return View(Array.Empty<Application.Features.Escritorio.Dtos.AgenteAdminDto>());
        }

        return View(result.Value);
    }

    [HttpGet("agentes/{id:guid}/editar")]
    public async Task<IActionResult> EditarAgente(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ObterAgenteAdminQuery(id), cancellationToken);
        if (result.IsFailure)
        {
            TempData["Erro"] = result.Error;
            return RedirectToAction(nameof(Agentes));
        }

        var a = result.Value;
        Enum.TryParse<SalaEscritorio>(a.Sala, out var sala);
        return View(new AgenteEscritorioFormViewModel
        {
            Id = a.Id,
            Slug = a.Slug,
            Nome = a.Nome,
            Papel = a.Papel,
            Sala = sala,
            Ordem = a.Ordem,
            Ativo = a.Ativo
        });
    }

    [HttpPost("agentes/{id:guid}/editar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarAgente(
        Guid id,
        AgenteEscritorioFormViewModel model,
        CancellationToken cancellationToken)
    {
        if (id != model.Id)
            return BadRequest();

        if (!ModelState.IsValid)
            return View(model);

        var result = await _mediator.Send(
            new AtualizarAgenteEscritorioCommand(
                model.Id, model.Nome, model.Papel, model.Sala, model.Ordem, model.Ativo),
            cancellationToken);

        if (result.IsFailure)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Falha ao salvar.");
            return View(model);
        }

        TempData["Ok"] = "Agente atualizado.";
        return RedirectToAction(nameof(Agentes));
    }
}
