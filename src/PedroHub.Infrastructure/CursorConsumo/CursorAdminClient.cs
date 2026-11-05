using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PedroHub.Application.Common.Interfaces;
using PedroHub.Application.Common.Options;
using PedroHub.Domain.CursorConsumo;
using PedroHub.Shared.Results;

namespace PedroHub.Infrastructure.CursorConsumo;

public sealed class CursorAdminClient : ICursorAdminClient
{
    public const int TamanhoPagina = 1000;
    public const int MaxPaginas = 50;
    public const string HostOficial = "api.cursor.com";

    private readonly HttpClient _http;
    private readonly CursorConsumoOptions _options;
    private readonly ILogger<CursorAdminClient> _logger;

    public CursorAdminClient(
        HttpClient http,
        IOptions<CursorConsumoOptions> options,
        ILogger<CursorAdminClient> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public bool EstaConfigurado => !string.IsNullOrWhiteSpace(_options.ApiKey);

    public async Task<Result<LeituraEventosCursor>> BuscarAsync(
        DateTime inicioUtcInclusive,
        DateTime fimUtcInclusive,
        CancellationToken cancellationToken = default)
    {
        if (!EstaConfigurado)
        {
            return Result.Failure<LeituraEventosCursor>(
                "API do Cursor não configurada. Defina HUB_CURSOR_API_KEY ou importe um CSV.");
        }

        if (!TryCriarEndpoint(out var endpoint, out var erroEndpoint))
            return Result.Failure<LeituraEventosCursor>(erroEndpoint!);

        var inicioMs = new DateTimeOffset(DateTime.SpecifyKind(inicioUtcInclusive, DateTimeKind.Utc)).ToUnixTimeMilliseconds();
        var fimMs = new DateTimeOffset(DateTime.SpecifyKind(fimUtcInclusive, DateTimeKind.Utc)).ToUnixTimeMilliseconds();
        var eventos = new List<EventoUsoNormalizado>();
        var truncado = false;

        for (var pagina = 1; pagina <= MaxPaginas; pagina++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Basic",
                Convert.ToBase64String(Encoding.UTF8.GetBytes(_options.ApiKey + ":")));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = JsonContent.Create(new
            {
                startDate = inicioMs,
                endDate = fimMs,
                page = pagina,
                pageSize = TamanhoPagina
            });

            HttpResponseMessage response;
            try
            {
                response = await _http.SendAsync(request, cancellationToken);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException
                                       && !cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "Falha de rede ao consultar a Admin API do Cursor.");
                return Result.Failure<LeituraEventosCursor>("Não foi possível consultar a Admin API do Cursor.");
            }

            using (response)
            {
                if (response.StatusCode is System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden)
                {
                    _logger.LogWarning("Admin API do Cursor recusou a chave ({StatusCode}).", (int)response.StatusCode);
                    return Result.Failure<LeituraEventosCursor>("A chave da Admin API do Cursor foi recusada.");
                }

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Admin API do Cursor respondeu {StatusCode}.", (int)response.StatusCode);
                    return Result.Failure<LeituraEventosCursor>(
                        $"A Admin API do Cursor respondeu {(int)response.StatusCode}.");
                }

                RespostaEventos? corpo;
                try
                {
                    corpo = await response.Content.ReadFromJsonAsync<RespostaEventos>(cancellationToken);
                }
                catch (System.Text.Json.JsonException ex)
                {
                    _logger.LogWarning(ex, "Resposta inválida da Admin API do Cursor.");
                    return Result.Failure<LeituraEventosCursor>("A Admin API do Cursor devolveu um JSON inesperado.");
                }

                if (corpo?.UsageEvents is { Count: > 0 })
                {
                    foreach (var item in corpo.UsageEvents)
                    {
                        if (Mapear(item) is { } evento)
                            eventos.Add(evento);
                    }
                }

                var temProxima = corpo?.Pagination?.HasNextPage == true && corpo.UsageEvents is { Count: > 0 };
                if (!temProxima)
                    return Result.Success(new LeituraEventosCursor(eventos, truncado));
            }
        }

        truncado = true;
        return Result.Success(new LeituraEventosCursor(eventos, truncado));
    }

    private bool TryCriarEndpoint(out Uri endpoint, out string? erro)
    {
        endpoint = null!;
        erro = null;
        var bruto = string.IsNullOrWhiteSpace(_options.BaseUrl)
            ? "https://api.cursor.com"
            : _options.BaseUrl.Trim();

        if (!Uri.TryCreate(bruto, UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttps
            || !string.Equals(uri.Host, HostOficial, StringComparison.OrdinalIgnoreCase)
            || !string.IsNullOrEmpty(uri.UserInfo))
        {
            erro = "CursorConsumo:BaseUrl precisa ser https://api.cursor.com.";
            return false;
        }

        endpoint = new Uri($"https://{HostOficial}/teams/filtered-usage-events");
        return true;
    }

    private static EventoUsoNormalizado? Mapear(EventoApi item)
    {
        if (string.IsNullOrWhiteSpace(item.Timestamp)
            || !long.TryParse(item.Timestamp, NumberStyles.Integer, CultureInfo.InvariantCulture, out var epochMs))
            return null;

        DateTime ocorrido;
        try
        {
            ocorrido = DateTimeOffset.FromUnixTimeMilliseconds(epochMs).UtcDateTime;
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }

        var entrada = item.TokenUsage?.InputTokens ?? 0;
        var saida = item.TokenUsage?.OutputTokens ?? 0;
        var cacheEscrita = item.TokenUsage?.CacheWriteTokens ?? 0;
        var cacheLeitura = item.TokenUsage?.CacheReadTokens ?? 0;
        var total = entrada + saida + cacheEscrita + cacheLeitura;
        var modelo = string.IsNullOrWhiteSpace(item.Model) ? "desconhecido" : item.Model.Trim();
        if (modelo.Length > 160)
            modelo = modelo[..160];

        var email = string.IsNullOrWhiteSpace(item.UserEmail) ? null : item.UserEmail.Trim();
        if (email is { Length: > 320 })
            email = email[..320];

        var tipo = string.IsNullOrWhiteSpace(item.Kind) ? null : item.Kind.Trim();
        if (tipo is { Length: > 80 })
            tipo = tipo[..80];

        return new EventoUsoNormalizado(
            ocorrido,
            email,
            modelo,
            tipo,
            item.MaxMode,
            entrada,
            cacheEscrita,
            cacheLeitura,
            saida,
            total,
            item.ChargedCents,
            item.RequestsCosts);
    }

    private sealed class RespostaEventos
    {
        [JsonPropertyName("usageEvents")]
        public List<EventoApi>? UsageEvents { get; set; }

        [JsonPropertyName("pagination")]
        public PaginacaoApi? Pagination { get; set; }
    }

    private sealed class PaginacaoApi
    {
        [JsonPropertyName("hasNextPage")]
        public bool HasNextPage { get; set; }
    }

    private sealed class EventoApi
    {
        [JsonPropertyName("timestamp")]
        public string? Timestamp { get; set; }

        [JsonPropertyName("userEmail")]
        public string? UserEmail { get; set; }

        [JsonPropertyName("model")]
        public string? Model { get; set; }

        [JsonPropertyName("kind")]
        public string? Kind { get; set; }

        [JsonPropertyName("maxMode")]
        public bool MaxMode { get; set; }

        [JsonPropertyName("requestsCosts")]
        public decimal? RequestsCosts { get; set; }

        [JsonPropertyName("chargedCents")]
        public decimal? ChargedCents { get; set; }

        [JsonPropertyName("tokenUsage")]
        public TokensApi? TokenUsage { get; set; }
    }

    private sealed class TokensApi
    {
        [JsonPropertyName("inputTokens")]
        public long InputTokens { get; set; }

        [JsonPropertyName("outputTokens")]
        public long OutputTokens { get; set; }

        [JsonPropertyName("cacheWriteTokens")]
        public long CacheWriteTokens { get; set; }

        [JsonPropertyName("cacheReadTokens")]
        public long CacheReadTokens { get; set; }
    }
}
