using System.Text.Json.Serialization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PedroHub.Application.Features.Escritorio.Commands;
using PedroHub.Web.Security;

namespace PedroHub.Web.Controllers;

[ApiController]
[AllowAnonymous]
[ServiceFilter(typeof(GideonApiKeyFilter))]
[EnableRateLimiting("gideon")]
[Route("api/gideon/agentes")]
[Consumes("application/json")]
public sealed class GideonAgentesController : ControllerBase
{
    private readonly IMediator _mediator;

    public GideonAgentesController(IMediator mediator) => _mediator = mediator;

    [HttpPost("status")]
    [RequestSizeLimit(65_536)]
    [RequestFormLimits(MultipartBodyLengthLimit = 65_536)]
    public async Task<IActionResult> Status(
        [FromBody] StatusIngestaoRequest? body,
        CancellationToken cancellationToken)
    {
        if (body?.Agentes is null)
            return BadRequest(new { erros = new[] { "JSON inválido: agentes obrigatório." } });

        var cmd = new IngerirStatusAgentesCommand(
            body.Agentes.Select(a => new StatusAgenteIngestaoItem(
                a.Slug ?? "",
                a.Status ?? "",
                a.TarefaAtual,
                a.Projeto,
                a.PrUrl,
                a.AtualizadoEm)).ToList());

        var result = await _mediator.Send(cmd, cancellationToken);
        if (result.IsFailure)
            return BadRequest(new { erros = new[] { result.Error } });

        return NoContent();
    }

    [HttpPost("eventos")]
    [RequestSizeLimit(65_536)]
    [RequestFormLimits(MultipartBodyLengthLimit = 65_536)]
    public async Task<IActionResult> Eventos(
        [FromBody] EventosIngestaoRequest? body,
        CancellationToken cancellationToken)
    {
        if (body?.Eventos is null)
            return BadRequest(new { erros = new[] { "JSON inválido: eventos obrigatório." } });

        var cmd = new IngerirEventosAgentesCommand(
            body.Eventos.Select(e => new EventoAgenteIngestaoItem(
                e.Slug ?? "",
                e.Tipo ?? "",
                e.Titulo ?? "",
                e.Descricao,
                e.Link,
                e.OcorridoEm)).ToList());

        var result = await _mediator.Send(cmd, cancellationToken);
        if (result.IsFailure)
            return BadRequest(new { erros = new[] { result.Error } });

        return NoContent();
    }

    public sealed class StatusIngestaoRequest
    {
        [JsonPropertyName("agentes")]
        public List<StatusItem>? Agentes { get; set; }
    }

    public sealed class StatusItem
    {
        [JsonPropertyName("slug")]
        public string? Slug { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("tarefaAtual")]
        public string? TarefaAtual { get; set; }

        [JsonPropertyName("projeto")]
        public string? Projeto { get; set; }

        [JsonPropertyName("prUrl")]
        public string? PrUrl { get; set; }

        [JsonPropertyName("atualizadoEm")]
        public DateTime AtualizadoEm { get; set; }
    }

    public sealed class EventosIngestaoRequest
    {
        [JsonPropertyName("eventos")]
        public List<EventoItem>? Eventos { get; set; }
    }

    public sealed class EventoItem
    {
        [JsonPropertyName("slug")]
        public string? Slug { get; set; }

        [JsonPropertyName("tipo")]
        public string? Tipo { get; set; }

        [JsonPropertyName("titulo")]
        public string? Titulo { get; set; }

        [JsonPropertyName("descricao")]
        public string? Descricao { get; set; }

        [JsonPropertyName("link")]
        public string? Link { get; set; }

        [JsonPropertyName("ocorridoEm")]
        public DateTime OcorridoEm { get; set; }
    }
}
