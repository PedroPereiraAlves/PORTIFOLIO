using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace PedroHub.Web.Tests;

[Collection("PedroHubWeb")]
public sealed class EscritorioDebugAttributeTests
{
    [Fact]
    public async Task Production_NaoRenderizaDataDebug()
    {
        await using var factory = new PedroHubWebApplicationFactory(environment: "Production");
        var html = await HtmlEscritorioAsync(factory);

        html.Should().Contain("id=\"escritorio-game\"");
        html.Should().NotContain("data-debug");
    }

    [Fact]
    public async Task Development_RenderizaDataDebugTrue()
    {
        await using var factory = new PedroHubWebApplicationFactory();
        var html = await HtmlEscritorioAsync(factory);

        html.Should().Contain("data-debug=\"true\"");
        Regex.IsMatch(html, "data-debug=\"\"").Should().BeFalse();
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Production")]
    public async Task Pagina_IncluiVersaoNosScriptsInjetadosPeloBoot(string environment)
    {
        await using var factory = new PedroHubWebApplicationFactory(environment: environment);
        var html = await HtmlEscritorioAsync(factory);

        var match = Regex.Match(html, "data-script-srcs=\"([^\"]*)\"");
        match.Success.Should().BeTrue();
        var json = WebUtility.HtmlDecode(match.Groups[1].Value);
        var srcs = JsonSerializer.Deserialize<string[]>(json);
        srcs.Should().NotBeNull();
        srcs.Should().HaveCount(6);

        string[] esperados =
        [
            "/lib/office/phaser/phaser.min.js",
            "/lib/office/easystar/easystar.min.js",
            "/js/escritorio/sprites.js",
            "/js/escritorio/agents.js",
            "/js/escritorio/ui-panel.js",
            "/js/escritorio/scene-office.js",
        ];
        for (var i = 0; i < esperados.Length; i++)
        {
            srcs![i].Should().StartWith(esperados[i] + "?v=");
            var versao = srcs[i][(esperados[i].Length + "?v=".Length)..];
            versao.Should().NotBeNullOrWhiteSpace();
            versao.Should().MatchRegex("^[A-Za-z0-9\\-._~%]+$");
        }
    }

    private static async Task<string> HtmlEscritorioAsync(PedroHubWebApplicationFactory factory)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        var loginPage = await client.GetAsync("/Account/Login");
        loginPage.EnsureSuccessStatusCode();
        var loginHtml = await loginPage.Content.ReadAsStringAsync();
        var token = ExtrairAntiForgery(loginHtml);

        var login = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = "owner-test@example.com",
            ["Senha"] = "OwnerTest1!",
            ["__RequestVerificationToken"] = token
        }));
        login.StatusCode.Should().BeOneOf(HttpStatusCode.Redirect, HttpStatusCode.Found, HttpStatusCode.SeeOther);

        var pagina = await client.GetAsync("/admin/escritorio");
        pagina.StatusCode.Should().Be(HttpStatusCode.OK);
        return await pagina.Content.ReadAsStringAsync();
    }

    private static string ExtrairAntiForgery(string html)
    {
        const string marker = "name=\"__RequestVerificationToken\"";
        var nameIdx = html.IndexOf(marker, StringComparison.Ordinal);
        nameIdx.Should().BeGreaterThanOrEqualTo(0);
        var valueIdx = html.IndexOf("value=\"", nameIdx, StringComparison.Ordinal);
        var start = valueIdx + "value=\"".Length;
        var end = html.IndexOf('"', start);
        return html[start..end];
    }
}
