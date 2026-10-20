using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PedroHub.Application.Features.Monitoramento.Commands;
using PedroHub.Application.Features.Monitoramento.Queries;
using PedroHub.Infrastructure.Identity;
using PedroHub.Web.Models;

namespace PedroHub.Web.Controllers;

[Authorize(Roles = OwnerAuthService.RoleOwner)]
[Route("admin/monitoramento")]
public class MonitoramentoController : Controller
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IMediator _mediator;

    public MonitoramentoController(IMediator mediator) => _mediator = mediator;

    [HttpGet("")]
    public async Task<IActionResult> Index(
        string? periodo,
        Guid? alvo,
        int pagina = 1,
        CancellationToken cancellationToken = default)
    {
        var resumo = await _mediator.Send(
            new ObterResumoMonitoramentoQuery(periodo ?? "24h", alvo),
            cancellationToken);

        if (resumo.IsFailure)
        {
            TempData["Erro"] = resumo.Error;
            return View(new MonitoramentoIndexViewModel
            {
                Resumo = new(Array.Empty<Application.Features.Monitoramento.Dtos.AlvoMonitoradoDto>(), null, null, [], "24h")
            });
        }

        var alvoHistorico = alvo ?? resumo.Value.Alvos.FirstOrDefault()?.Id;
        ListaChecagensDto? historico = null;
        if (alvoHistorico is Guid id)
        {
            var hist = await _mediator.Send(new ListarChecagensQuery(id, pagina), cancellationToken);
            if (hist.IsSuccess)
                historico = hist.Value;
        }

        var dados = resumo.Value.SerieLatencia.Select(p => new
        {
            em = p.Em.ToString("o"),
            latenciaMs = p.LatenciaMs,
            status = p.Status.ToString()
        });

        return View(new MonitoramentoIndexViewModel
        {
            Resumo = resumo.Value,
            AlvoHistoricoId = alvoHistorico,
            Historico = historico,
            DadosLatenciaJson = JsonSerializer.Serialize(dados, JsonOpts)
        });
    }

    [HttpGet("novo")]
    public async Task<IActionResult> Criar(CancellationToken cancellationToken)
    {
        await CarregarProjetosAsync(cancellationToken);
        return View("Form", new AlvoMonitoradoFormViewModel());
    }

    [HttpPost("novo")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Criar(AlvoMonitoradoFormViewModel model, CancellationToken cancellationToken)
    {
        await CarregarProjetosAsync(cancellationToken);
        if (!ModelState.IsValid)
            return View("Form", model);

        var result = await _mediator.Send(new CriarAlvoMonitoradoCommand(
            model.Nome, model.Url, model.Metodo, model.IntervaloSegundos, model.TimeoutMs,
            model.StatusEsperado, model.LimiarDegradadoMs, model.Ativo, model.ProjetoId, model.Infra),
            cancellationToken);

        if (result.IsFailure)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View("Form", model);
        }

        TempData["Sucesso"] = "Alvo criado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("{id:guid}/editar")]
    public async Task<IActionResult> Editar(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ObterAlvoPorIdQuery(id), cancellationToken);
        if (result.IsFailure)
        {
            TempData["Erro"] = result.Error;
            return RedirectToAction(nameof(Index));
        }

        await CarregarProjetosAsync(cancellationToken);
        var a = result.Value;
        return View("Form", new AlvoMonitoradoFormViewModel
        {
            Id = a.Id,
            Nome = a.Nome,
            Url = a.Url,
            Metodo = a.Metodo,
            IntervaloSegundos = a.IntervaloSegundos,
            TimeoutMs = a.TimeoutMs,
            StatusEsperado = a.StatusEsperado,
            LimiarDegradadoMs = a.LimiarDegradadoMs,
            Ativo = a.Ativo,
            ProjetoId = a.ProjetoId,
            Infra = a.Infra
        });
    }

    [HttpPost("{id:guid}/editar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(Guid id, AlvoMonitoradoFormViewModel model, CancellationToken cancellationToken)
    {
        model.Id = id;
        await CarregarProjetosAsync(cancellationToken);
        if (!ModelState.IsValid)
            return View("Form", model);

        var result = await _mediator.Send(new AtualizarAlvoMonitoradoCommand(
            id, model.Nome, model.Url, model.Metodo, model.IntervaloSegundos, model.TimeoutMs,
            model.StatusEsperado, model.LimiarDegradadoMs, model.Ativo, model.ProjetoId, model.Infra),
            cancellationToken);

        if (result.IsFailure)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View("Form", model);
        }

        TempData["Sucesso"] = "Alvo atualizado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:guid}/ativar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AlternarAtivo(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new AlternarAtivoAlvoCommand(id), cancellationToken);
        TempData[result.IsSuccess ? "Sucesso" : "Erro"] = result.IsSuccess
            ? "Estado do alvo atualizado."
            : result.Error;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:guid}/excluir")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Excluir(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ExcluirAlvoMonitoradoCommand(id), cancellationToken);
        TempData[result.IsSuccess ? "Sucesso" : "Erro"] = result.IsSuccess
            ? "Alvo excluído."
            : result.Error;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:guid}/checar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChecarAgora(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ExecutarChecagemCommand(id), cancellationToken);
        if (result.IsFailure)
            TempData["Erro"] = result.Error;
        else
            TempData["Sucesso"] =
                $"Checagem: {result.Value.Status} · {result.Value.LatenciaMs} ms"
                + (result.Value.CodigoHttp is int c ? $" · HTTP {c}" : "");

        return RedirectToAction(nameof(Index), new { alvo = id });
    }

    private async Task CarregarProjetosAsync(CancellationToken cancellationToken)
    {
        var projetos = await _mediator.Send(new ListarProjetosSelectQuery(), cancellationToken);
        ViewBag.Projetos = projetos.IsSuccess
            ? new SelectList(projetos.Value, "Id", "Nome")
            : new SelectList(Array.Empty<object>());
    }
}
