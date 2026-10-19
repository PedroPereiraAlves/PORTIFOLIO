using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PedroHub.Application.Common.Interfaces;
using PedroHub.Application.Common.Options;
using PedroHub.Domain.Enums;

namespace PedroHub.Infrastructure.Alertas;

public sealed class ResendAlertaService : IAlertaService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ResendOptions _resend;
    private readonly MonitoramentoOptions _monitoramento;
    private readonly ILogger<ResendAlertaService> _logger;
    private readonly ConcurrentDictionary<Guid, DateTime> _ultimoPorAlvo = new();
    private readonly ConcurrentQueue<DateTime> _enviosHora = new();

    public ResendAlertaService(
        IHttpClientFactory httpClientFactory,
        IOptions<ResendOptions> resend,
        IOptions<MonitoramentoOptions> monitoramento,
        ILogger<ResendAlertaService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _resend = resend.Value;
        _monitoramento = monitoramento.Value;
        _logger = logger;
    }

    public async Task NotificarMudancaStatusAsync(
        Guid alvoId,
        string nomeAlvo,
        string url,
        StatusAlvo statusAnterior,
        StatusAlvo statusNovo,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_resend.ApiKey))
        {
            _logger.LogInformation(
                "Alerta não enviado: Resend não configurado. Alvo={Alvo} {De}→{Para}",
                nomeAlvo, statusAnterior, statusNovo);
            return;
        }

        var de = _monitoramento.Alertas.De;
        var para = _monitoramento.Alertas.Para;
        if (string.IsNullOrWhiteSpace(de) || string.IsNullOrWhiteSpace(para))
        {
            _logger.LogWarning(
                "Alerta não enviado: Monitoramento:Alertas:De/Para ausentes. Destinatario={Dest}",
                MascararEmail(para));
            return;
        }

        var agora = DateTime.UtcNow;
        var minutos = Math.Max(1, _monitoramento.Alertas.MinutosEntreEmailsPorAlvo);
        if (_ultimoPorAlvo.TryGetValue(alvoId, out var ultimo)
            && agora - ultimo < TimeSpan.FromMinutes(minutos))
        {
            _logger.LogInformation(
                "Alerta rate-limited por alvo. Alvo={Alvo} Destinatario={Dest}",
                nomeAlvo, MascararEmail(para));
            return;
        }

        LimparFilaHora(agora);
        var maxHora = Math.Max(1, _monitoramento.Alertas.MaxEmailsPorHora);
        if (_enviosHora.Count >= maxHora)
        {
            _logger.LogInformation(
                "Alerta rate-limited global (hora). Destinatario={Dest}",
                MascararEmail(para));
            return;
        }

        var caiu = statusNovo is StatusAlvo.Offline or StatusAlvo.Degradado;
        var assunto = caiu
            ? $"[PedroHub] Alvo caiu: {nomeAlvo}"
            : $"[PedroHub] Alvo voltou: {nomeAlvo}";
        var corpo =
            $"Alvo: {nomeAlvo}\nURL: {url}\nStatus: {statusAnterior} → {statusNovo}\nQuando (UTC): {agora:O}";

        try
        {
            var client = _httpClientFactory.CreateClient("resend");
            using var msg = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails");
            msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _resend.ApiKey);
            msg.Content = JsonContent.Create(new ResendEmailRequest(de, [para], assunto, corpo));

            using var response = await client.SendAsync(msg, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Falha ao enviar alerta Resend. Status={Status} Destinatario={Dest}",
                    (int)response.StatusCode, MascararEmail(para));
                return;
            }

            _ultimoPorAlvo[alvoId] = agora;
            _enviosHora.Enqueue(agora);
            _logger.LogInformation(
                "Alerta enviado via Resend. Alvo={Alvo} Destinatario={Dest} {De}→{Para}",
                nomeAlvo, MascararEmail(para), statusAnterior, statusNovo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enviar alerta Resend. Destinatario={Dest}", MascararEmail(para));
        }
    }

    private void LimparFilaHora(DateTime agora)
    {
        while (_enviosHora.TryPeek(out var t) && agora - t > TimeSpan.FromHours(1))
            _enviosHora.TryDequeue(out _);
    }

    public static string MascararEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            return "***";
        var partes = email.Split('@', 2);
        var user = partes[0];
        var masked = user.Length <= 2 ? "**" : user[0] + new string('*', user.Length - 2) + user[^1];
        return $"{masked}@{partes[1]}";
    }

    private sealed record ResendEmailRequest(
        [property: JsonPropertyName("from")] string From,
        [property: JsonPropertyName("to")] string[] To,
        [property: JsonPropertyName("subject")] string Subject,
        [property: JsonPropertyName("text")] string Text);
}
