using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PedroHub.Application.Common.Options;
using PedroHub.Infrastructure.CursorConsumo;

namespace PedroHub.Application.Tests.Features.CursorConsumo;

public sealed class CursorAdminClientTests
{
    [Fact]
    public async Task SemChave_NaoChamaARede()
    {
        var handler = new FilaHandler();
        var client = Criar(handler, apiKey: null);

        client.EstaConfigurado.Should().BeFalse();
        var resultado = await client.BuscarAsync(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow);

        resultado.IsFailure.Should().BeTrue();
        handler.Chamadas.Should().Be(0);
    }

    [Fact]
    public async Task MapeiaEventoOficial_E_Pagina()
    {
        var handler = new FilaHandler();
        handler.Enfileirar(HttpStatusCode.OK, """
            {
              "usageEvents": [
                {
                  "timestamp": "1750979225854",
                  "userEmail": "developer@company.com",
                  "model": "claude-4.5-sonnet",
                  "kind": "Usage-based",
                  "maxMode": true,
                  "requestsCosts": 5,
                  "chargedCents": 21.36232,
                  "tokenUsage": {
                    "inputTokens": 126,
                    "outputTokens": 450,
                    "cacheWriteTokens": 6112,
                    "cacheReadTokens": 11964
                  }
                }
              ],
              "pagination": { "hasNextPage": true }
            }
            """);
        handler.Enfileirar(HttpStatusCode.OK, """
            { "usageEvents": [], "pagination": { "hasNextPage": false } }
            """);
        var client = Criar(handler, "chave-teste");

        var resultado = await client.BuscarAsync(
            new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 5, 8, 0, 0, 0, DateTimeKind.Utc));

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Truncado.Should().BeFalse();
        var evento = resultado.Value.Eventos.Should().ContainSingle().Subject;
        evento.Modelo.Should().Be("claude-4.5-sonnet");
        evento.TokensEntrada.Should().Be(126);
        evento.TokensSaida.Should().Be(450);
        evento.TokensCacheEscrita.Should().Be(6112);
        evento.TokensCacheLeitura.Should().Be(11964);
        evento.TokensTotal.Should().Be(126 + 450 + 6112 + 11964);
        evento.CustoCentavos.Should().Be(21.36232m);
        evento.UnidadesRequisicao.Should().Be(5);
        evento.ModoMaximo.Should().BeTrue();
        evento.OcorridoEmUtc.Kind.Should().Be(DateTimeKind.Utc);

        handler.Chamadas.Should().Be(2);
        handler.Urls.Should().OnlyContain(u => u == "https://api.cursor.com/teams/filtered-usage-events");
        handler.Autorizacoes.Should().OnlyContain(a => a == Basic("chave-teste"));
        handler.Corpos[0].Should().Contain("\"page\":1");
        handler.Corpos[0].Should().NotContain("chave-teste");
    }

    [Fact]
    public async Task ChaveRecusada_401()
    {
        var handler = new FilaHandler();
        handler.Enfileirar(HttpStatusCode.Unauthorized, "{}");
        var client = Criar(handler, "chave-ruim");

        var resultado = await client.BuscarAsync(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Contain("recusada");
    }

    [Fact]
    public async Task HostForaDoOficial_NaoChamaARede()
    {
        var handler = new FilaHandler();
        var client = Criar(handler, "chave", "http://127.0.0.1:9");

        var resultado = await client.BuscarAsync(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow);

        resultado.IsFailure.Should().BeTrue();
        handler.Chamadas.Should().Be(0);
    }

    [Fact]
    public async Task FalhaDeRede_ViraResultado()
    {
        var handler = new FilaHandler { FalharRede = true };
        var client = Criar(handler, "chave");

        var resultado = await client.BuscarAsync(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Contain("Não foi possível");
    }

    private static CursorAdminClient Criar(FilaHandler handler, string? apiKey, string? baseUrl = null)
    {
        var http = new HttpClient(handler);
        var options = Options.Create(new CursorConsumoOptions
        {
            ApiKey = apiKey,
            BaseUrl = baseUrl ?? "https://api.cursor.com"
        });
        return new CursorAdminClient(http, options, NullLogger<CursorAdminClient>.Instance);
    }

    private static string Basic(string key) =>
        "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(key + ":"));

    private sealed class FilaHandler : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _respostas = new();
        public bool FalharRede { get; init; }
        public int Chamadas { get; private set; }
        public List<string> Urls { get; } = [];
        public List<string?> Autorizacoes { get; } = [];
        public List<string> Corpos { get; } = [];

        public void Enfileirar(HttpStatusCode status, string json)
        {
            _respostas.Enqueue(new HttpResponseMessage(status)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Chamadas++;
            if (FalharRede)
                throw new HttpRequestException("falha simulada");

            Urls.Add(request.RequestUri?.ToString() ?? "");
            Autorizacoes.Add(request.Headers.Authorization?.ToString());
            Corpos.Add(request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken));
            return _respostas.Dequeue();
        }
    }
}
