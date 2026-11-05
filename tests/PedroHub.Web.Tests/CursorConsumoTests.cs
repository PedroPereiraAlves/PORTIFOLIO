using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace PedroHub.Web.Tests;

[Collection("PedroHubWeb")]
public sealed class CursorConsumoTests
{
    [Fact]
    public async Task Anonimo_AdminCursor_RedirecionaParaLogin()
    {
        await using var factory = new PedroHubWebApplicationFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var resposta = await client.GetAsync("/admin/cursor");

        resposta.StatusCode.Should().Be(HttpStatusCode.Redirect);
        resposta.Headers.Location!.OriginalString.Should().Contain("/Account/Login");
    }

    [Fact]
    public async Task HomePublica_NaoApontaParaConsumo()
    {
        await using var factory = new PedroHubWebApplicationFactory();
        var client = factory.CreateClient();

        var html = await client.GetStringAsync("/");

        html.Should().NotContain("/admin/cursor");
        html.Should().NotContain("Consumo Cursor");
    }

    [Fact]
    public async Task Owner_VeTela_ImportaCsv_E_NaoTemScriptInline()
    {
        await using var factory = new PedroHubWebApplicationFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        await EntrarAsync(client);

        var pagina = await client.GetAsync("/admin/cursor");
        pagina.StatusCode.Should().Be(HttpStatusCode.OK);
        pagina.Headers.GetValues("Content-Security-Policy").Single().Should().Contain("script-src 'self'");
        var html = await pagina.Content.ReadAsStringAsync();
        html.Should().Contain("Consumo Cursor");
        html.Should().Contain("Importar CSV");
        html.Should().Contain("id=\"grafico-consumo\"");
        html.Should().Contain("id=\"dados-consumo\"");
        html.Should().Contain("/js/cursor-consumo.js");
        html.Should().NotContain("HUB_CURSOR_API_KEY=");
        ScriptsSaoExternosOuJson(html);

        var quando = DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd'T'HH:mm:ss'Z'");
        var csv =
            "Date,User,Model,Total Tokens,Cost\n" +
            $"{quando},pedro@exemplo.com,grok,1250,$1.50\n";
        var token = Token(html);
        using var corpo = new MultipartFormDataContent();
        corpo.Add(new StringContent(token), "__RequestVerificationToken");
        corpo.Add(new StringContent("30d"), "periodo");
        var arquivo = new ByteArrayContent(Encoding.UTF8.GetBytes(csv));
        arquivo.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        corpo.Add(arquivo, "arquivo", "uso.csv");

        var importacao = await client.PostAsync("/admin/cursor/importar", corpo);
        importacao.StatusCode.Should().Be(HttpStatusCode.Redirect);
        importacao.Headers.Location!.OriginalString.Should().Contain("/admin/cursor");

        var depois = await client.GetStringAsync("/admin/cursor?periodo=30d");
        depois.Should().Contain("1.250");
        depois.Should().Contain("grok");
        depois.Should().Contain("US$ 1,50");
        ScriptsSaoExternosOuJson(depois);
    }

    [Fact]
    public async Task Anonimo_NaoImporta()
    {
        await using var factory = new PedroHubWebApplicationFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var corpo = new MultipartFormDataContent();
        corpo.Add(new ByteArrayContent("Date,Model\n"u8.ToArray()), "arquivo", "uso.csv");

        var resposta = await client.PostAsync("/admin/cursor/importar", corpo);

        resposta.StatusCode.Should().Be(HttpStatusCode.Redirect);
        resposta.Headers.Location!.OriginalString.Should().Contain("/Account/Login");
    }

    private static async Task EntrarAsync(HttpClient client)
    {
        var login = await client.GetAsync("/Account/Login");
        login.EnsureSuccessStatusCode();
        var html = await login.Content.ReadAsStringAsync();
        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = "owner-test@example.com",
            ["Senha"] = "OwnerTest1!",
            ["__RequestVerificationToken"] = Token(html)
        });

        var resposta = await client.PostAsync("/Account/Login", form);
        resposta.StatusCode.Should().Be(HttpStatusCode.Redirect);
    }

    private static string Token(string html)
    {
        var match = Regex.Match(
            html,
            "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"|value=\"([^\"]+)\"[^>]*name=\"__RequestVerificationToken\"");
        match.Success.Should().BeTrue();
        return match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
    }

    private static void ScriptsSaoExternosOuJson(string html)
    {
        var tags = Regex.Matches(html, "<script\\b[^>]*>", RegexOptions.IgnoreCase);
        tags.Should().NotBeEmpty();
        foreach (Match tag in tags)
        {
            var texto = tag.Value;
            var externo = texto.Contains("src=", StringComparison.OrdinalIgnoreCase);
            var json = texto.Contains("type=\"application/json\"", StringComparison.OrdinalIgnoreCase);
            (externo || json).Should().BeTrue($"script inesperado: {texto}");
        }
    }
}
