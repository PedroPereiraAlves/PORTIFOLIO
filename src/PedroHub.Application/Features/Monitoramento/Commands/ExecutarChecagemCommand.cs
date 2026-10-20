using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PedroHub.Application.Common.Interfaces;
using PedroHub.Application.Common.Options;
using PedroHub.Domain.Entities;
using PedroHub.Domain.Enums;
using PedroHub.Domain.Monitoramento;
using PedroHub.Shared.Results;

namespace PedroHub.Application.Features.Monitoramento.Commands;

public sealed record ExecutarChecagemCommand(Guid AlvoId) : IRequest<Result<Checagem>>;

public sealed class ExecutarChecagemCommandHandler
    : IRequestHandler<ExecutarChecagemCommand, Result<Checagem>>
{
    private readonly IApplicationDbContext _db;
    private readonly IUnitOfWork _uow;
    private readonly IValidadorDestinoSeguro _validador;
    private readonly IChecagemHttpClient _http;
    private readonly IAlertaService _alertas;
    private readonly MonitoramentoOptions _options;
    private readonly ILogger<ExecutarChecagemCommandHandler> _logger;

    public ExecutarChecagemCommandHandler(
        IApplicationDbContext db,
        IUnitOfWork uow,
        IValidadorDestinoSeguro validador,
        IChecagemHttpClient http,
        IAlertaService alertas,
        IOptions<MonitoramentoOptions> options,
        ILogger<ExecutarChecagemCommandHandler> logger)
    {
        _db = db;
        _uow = uow;
        _validador = validador;
        _http = http;
        _alertas = alertas;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<Result<Checagem>> Handle(ExecutarChecagemCommand request, CancellationToken cancellationToken)
    {
        var alvo = await _db.AlvosMonitorados.FirstOrDefaultAsync(a => a.Id == request.AlvoId, cancellationToken);
        if (alvo is null)
            return Result.Failure<Checagem>("Alvo não encontrado.");

        var agora = DateTime.UtcNow;
        var statusAnterior = alvo.StatusAtual;

        var validacao = await _validador.ValidarAsync(alvo.Url, cancellationToken);
        ResultadoChecagemHttp resultadoHttp;

        if (!validacao.EhSeguro || validacao.UriNormalizada is null || validacao.IpsPermitidos is null)
        {
            resultadoHttp = new ResultadoChecagemHttp(
                SucessoHttp: false,
                CodigoHttp: null,
                LatenciaMs: 0,
                TimeoutOuErroRede: true,
                Erro: Truncar(validacao.Motivo ?? "Destino rejeitado pelo validador SSRF."));
        }
        else
        {
            resultadoHttp = await _http.ExecutarAsync(
                validacao.UriNormalizada,
                alvo.Metodo,
                alvo.TimeoutMs,
                validacao.IpsPermitidos,
                cancellationToken);
        }

        var avaliacao = RegraStatusAlvo.Avaliar(new RegraStatusAlvo.Entrada(
            resultadoHttp,
            alvo.StatusEsperado,
            alvo.LimiarDegradadoMs,
            alvo.FalhasConsecutivas,
            statusAnterior,
            _options.FalhasParaOffline));

        var checagem = new Checagem
        {
            AlvoMonitoradoId = alvo.Id,
            ExecutadaEm = agora,
            Status = avaliacao.NovoStatus,
            LatenciaMs = resultadoHttp.LatenciaMs,
            CodigoHttp = resultadoHttp.CodigoHttp,
            Erro = Truncar(resultadoHttp.Erro)
        };

        alvo.StatusAtual = avaliacao.NovoStatus;
        alvo.FalhasConsecutivas = avaliacao.NovasFalhasConsecutivas;
        alvo.UltimaChecagemEm = agora;
        alvo.ProximaChecagemEm = agora.AddSeconds(alvo.IntervaloSegundos);

        if (avaliacao.StatusMudou)
            alvo.UltimaMudancaStatusEm = agora;

        _db.Checagens.Add(checagem);
        await _uow.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Checagem concluída. AlvoId={AlvoId} Status={Status} LatenciaMs={Latencia} CodigoHttp={Codigo} TraceId={TraceId}",
            alvo.Id,
            checagem.Status,
            checagem.LatenciaMs,
            checagem.CodigoHttp,
            System.Diagnostics.Activity.Current?.Id);

        if (avaliacao.StatusMudou
            && statusAnterior != StatusAlvo.Desconhecido
            && DeveAlertar(statusAnterior, avaliacao.NovoStatus))
        {
            await _alertas.NotificarMudancaStatusAsync(
                alvo.Id,
                alvo.Nome,
                alvo.Url,
                statusAnterior,
                avaliacao.NovoStatus,
                cancellationToken);
        }

        return Result.Success(checagem);
    }

    /// <summary>
    /// Alerta de queda só quando chega em Offline (≥2 falhas).
    /// Online ⇄ Degradado fica só no dashboard/log.
    /// Recuperação: Offline → Online/Degradado.
    /// </summary>
    private static bool DeveAlertar(StatusAlvo anterior, StatusAlvo novo)
    {
        if (novo == StatusAlvo.Offline && anterior != StatusAlvo.Offline)
            return true;

        if (anterior == StatusAlvo.Offline
            && novo is StatusAlvo.Online or StatusAlvo.Degradado)
            return true;

        return false;
    }

    private static string? Truncar(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return null;
        texto = texto.Trim();
        return texto.Length <= 500 ? texto : texto[..500];
    }
}
