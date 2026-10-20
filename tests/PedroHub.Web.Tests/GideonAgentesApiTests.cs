using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using PedroHub.Web.Security;

namespace PedroHub.Web.Tests;

public sealed class GideonAgentesApiTests
{
    private static HttpClient CriarCliente(PedroHubWebApplicationFactory factory, bool comChave = true)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        if (comChave)
            client.DefaultRequestHeaders.Add(GideonApiKeyFilter.HeaderName, "chave-fake-dev");
        return client;
    }

    private static StringContent Json(string body) =>
        new(body, Encoding.UTF8, "application/json");

    [Fact]
    public async Task SemHeader_401()
    {
        await using var factory = new PedroHubWebApplicationFactory();
        var client = CriarCliente(factory, comChave: false);
        var res = await client.PostAsync(
            "/api/gideon/agentes/status",
            Json("""{"agentes":[{"slug":"front-hub","status":"trabalhando"}]}"""));
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ChaveErrada_401()
    {
        await using var factory = new PedroHubWebApplicationFactory();
        var client = CriarCliente(factory, comChave: false);
        client.DefaultRequestHeaders.Add(GideonApiKeyFilter.HeaderName, "errada");
        var res = await client.PostAsync(
            "/api/gideon/agentes/status",
            Json("""{"agentes":[{"slug":"front-hub","status":"trabalhando"}]}"""));
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SemChaveConfigurada_503()
    {
        await using var factory = new PedroHubWebApplicationFactory(gideonApiKey: null);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(GideonApiKeyFilter.HeaderName, "qualquer");
        var res = await client.PostAsync(
            "/api/gideon/agentes/status",
            Json("""{"agentes":[{"slug":"front-hub","status":"trabalhando"}]}"""));
        res.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task SemChave_JsonInvalido_401_Nao400()
    {
        await using var factory = new PedroHubWebApplicationFactory();
        var client = CriarCliente(factory, comChave: false);
        var res = await client.PostAsync(
            "/api/gideon/agentes/status",
            Json("{isto-nao-e-json"));
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ChaveCerta_JsonInvalido_400()
    {
        await using var factory = new PedroHubWebApplicationFactory();
        var client = CriarCliente(factory);
        var res = await client.PostAsync(
            "/api/gideon/agentes/status",
            Json("{isto-nao-e-json"));
        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ChaveCerta_SlugInexistente_400()
    {
        await using var factory = new PedroHubWebApplicationFactory();
        var client = CriarCliente(factory);
        var res = await client.PostAsync(
            "/api/gideon/agentes/status",
            Json("""{"agentes":[{"slug":"nao-existe","status":"trabalhando"}]}"""));
        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ChaveCerta_StatusInvalido_400()
    {
        await using var factory = new PedroHubWebApplicationFactory();
        var client = CriarCliente(factory);
        var res = await client.PostAsync(
            "/api/gideon/agentes/status",
            Json("""{"agentes":[{"slug":"front-hub","status":"voando"}]}"""));
        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ChaveCerta_PayloadValido_204()
    {
        await using var factory = new PedroHubWebApplicationFactory();
        var client = CriarCliente(factory);
        var agora = DateTime.UtcNow.ToString("o");
        var body =
            "{\"agentes\":[{\"slug\":\"front-hub\",\"status\":\"trabalhando\",\"tarefaAtual\":\"teste\",\"projeto\":\"Hub\",\"atualizadoEm\":\"" +
            agora +
            "\"}]}";
        var res = await client.PostAsync("/api/gideon/agentes/status", Json(body));
        res.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ChaveCerta_CorpoGrande_413()
    {
        await using var factory = new PedroHubWebApplicationFactory();
        var client = CriarCliente(factory);
        var padding = new string('x', 70_000);
        var body =
            "{\"agentes\":[{\"slug\":\"front-hub\",\"status\":\"trabalhando\",\"tarefaAtual\":\"" +
            padding +
            "\"}]}";
        var res = await client.PostAsync("/api/gideon/agentes/status", Json(body));
        res.StatusCode.Should().Be(HttpStatusCode.RequestEntityTooLarge);
    }

    [Fact]
    public async Task CookieOwner_SemHeader_401()
    {
        await using var factory = new PedroHubWebApplicationFactory();
        var client = CriarCliente(factory, comChave: false);
        await LoginOwnerAsync(client);
        var res = await client.PostAsync(
            "/api/gideon/agentes/status",
            Json("""{"agentes":[{"slug":"front-hub","status":"trabalhando"}]}"""));
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetEstado_Anonimo_302_SemJson()
    {
        await using var factory = new PedroHubWebApplicationFactory();
        var client = CriarCliente(factory, comChave: false);
        var res = await client.GetAsync("/admin/escritorio/estado");
        res.StatusCode.Should().Be(HttpStatusCode.Redirect);
        res.Headers.Location.Should().NotBeNull();
        res.Headers.Location!.ToString().Should().Contain("/Account/Login");
        var body = await res.Content.ReadAsStringAsync();
        body.TrimStart().Should().NotStartWith("{");
    }

    [Fact]
    public async Task RateLimit_31PorMinuto_429()
    {
        await using var factory = new PedroHubWebApplicationFactory();
        var client = CriarCliente(factory);
        HttpStatusCode? last = null;
        for (var i = 0; i < 31; i++)
        {
            var res = await client.PostAsync(
                "/api/gideon/agentes/status",
                Json("""{"agentes":[{"slug":"front-hub","status":"trabalhando"}]}"""));
            last = res.StatusCode;
            if (res.StatusCode == HttpStatusCode.TooManyRequests)
                break;
        }

        last.Should().Be(HttpStatusCode.TooManyRequests);
    }

    private static async Task LoginOwnerAsync(HttpClient client)
    {
        var loginPage = await client.GetAsync("/Account/Login");
        loginPage.EnsureSuccessStatusCode();
        var html = await loginPage.Content.ReadAsStringAsync();
        var token = ExtrairAntiForgery(html);

        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = "owner-test@example.com",
            ["Senha"] = "OwnerTest1!",
            ["__RequestVerificationToken"] = token
        });

        var login = await client.PostAsync("/Account/Login", form);
        login.StatusCode.Should().BeOneOf(HttpStatusCode.Redirect, HttpStatusCode.Found, HttpStatusCode.SeeOther);
    }

    private static string ExtrairAntiForgery(string html)
    {
        const string name = "name=\"__RequestVerificationToken\"";
        var nameIdx = html.IndexOf(name, StringComparison.Ordinal);
        nameIdx.Should().BeGreaterThanOrEqualTo(0, "token antiforgery no login");

        var valueIdx = html.IndexOf("value=\"", nameIdx, StringComparison.Ordinal);
        valueIdx.Should().BeGreaterThan(nameIdx);
        var start = valueIdx + "value=\"".Length;
        var end = html.IndexOf('"', start);
        return html[start..end];
    }
}
