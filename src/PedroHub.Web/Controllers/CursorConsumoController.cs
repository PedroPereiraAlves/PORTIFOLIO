using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PedroHub.Application.Features.CursorConsumo.Commands;
using PedroHub.Application.Features.CursorConsumo.Dtos;
using PedroHub.Application.Features.CursorConsumo.Queries;
using PedroHub.Infrastructure.Identity;
using PedroHub.Web.Models;

namespace PedroHub.Web.Controllers;

[Authorize(Roles = OwnerAuthService.RoleOwner)]
[Route("admin/cursor")]
public class CursorConsumoController : Controller
{
    public const long MaxCsvBytes = 4_000_000;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.Default
    };

    private readonly IMediator _mediator;

    public CursorConsumoController(IMediator mediator) => _mediator = mediator;

    [HttpGet("")]
    public async Task<IActionResult> Index(string? periodo, CancellationToken cancellationToken)
    {
        var resumo = await _mediator.Send(new ObterConsumoCursorQuery(periodo), cancellationToken);
        if (resumo.IsFailure)
        {
            TempData["Erro"] = resumo.Error;
            return View(new ConsumoCursorIndexViewModel());
        }

        var dados = resumo.Value.PorDia.Select(p => new
        {
            dia = p.Dia.ToString("dd/MM"),
            tokens = p.Tokens,
            requisicoes = p.Requisicoes,
            custoUsd = p.CustoCentavos is null ? (decimal?)null : decimal.Round(p.CustoCentavos.Value / 100m, 4)
        });

        return View(new ConsumoCursorIndexViewModel
        {
            Resumo = resumo.Value,
            DadosGraficoJson = JsonSerializer.Serialize(dados, JsonOpts)
        });
    }

    [HttpPost("importar")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(MaxCsvBytes + 100_000)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxCsvBytes + 100_000)]
    public async Task<IActionResult> Importar(IFormFile? arquivo, string? periodo, CancellationToken cancellationToken)
    {
        if (arquivo is null || arquivo.Length == 0)
        {
            TempData["Erro"] = "Escolha um arquivo CSV.";
            return RedirectToAction(nameof(Index), new { periodo });
        }

        if (arquivo.Length > MaxCsvBytes)
        {
            TempData["Erro"] = "O CSV passa de 4 MB.";
            return RedirectToAction(nameof(Index), new { periodo });
        }

        if (!string.Equals(Path.GetExtension(arquivo.FileName), ".csv", StringComparison.OrdinalIgnoreCase))
        {
            TempData["Erro"] = "Envie um arquivo .csv.";
            return RedirectToAction(nameof(Index), new { periodo });
        }

        string conteudo;
        await using (var stream = arquivo.OpenReadStream())
        using (var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
        {
            conteudo = await reader.ReadToEndAsync(cancellationToken);
        }

        var nome = Path.GetFileName(arquivo.FileName);
        var result = await _mediator.Send(new ImportarCsvUsoCursorCommand(conteudo, nome), cancellationToken);
        if (result.IsFailure)
        {
            TempData["Erro"] = result.Error;
            return RedirectToAction(nameof(Index), new { periodo });
        }

        var r = result.Value;
        var mensagem =
            $"CSV importado: {r.Inseridos} novos, {r.Duplicados} já existentes, {r.Ignorados} ignorados.";
        if (!string.IsNullOrWhiteSpace(r.Aviso))
            mensagem += " " + r.Aviso;
        TempData["Sucesso"] = mensagem;

        return RedirectToAction(nameof(Index), new { periodo });
    }

    [HttpPost("sincronizar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Sincronizar(string? periodo, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new SincronizarUsoCursorApiCommand(90), cancellationToken);
        if (result.IsFailure)
            TempData["Erro"] = result.Error;
        else
        {
            var r = result.Value;
            var mensagem = $"Admin API: {r.Inseridos} novos, {r.Duplicados} já existentes.";
            if (!string.IsNullOrWhiteSpace(r.Aviso))
                mensagem += " " + r.Aviso;
            TempData["Sucesso"] = mensagem;
        }

        return RedirectToAction(nameof(Index), new { periodo });
    }
}
