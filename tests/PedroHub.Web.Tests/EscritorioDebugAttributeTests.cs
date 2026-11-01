using System.Net;
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
