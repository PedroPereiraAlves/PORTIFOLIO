using FluentAssertions;
using PedroHub.Domain.Escritorio;

namespace PedroHub.Application.Tests.Features.Escritorio;

public sealed class GithubPrUrlRulesTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Vazia_EhPermitida(string? url) =>
        GithubPrUrlRules.EhPermitida(url).Should().BeTrue();

    [Fact]
    public void UrlValida_EhPermitida() =>
        GithubPrUrlRules.EhPermitida(
                "https://github.com/PedroPereiraAlves/PORTIFOLIO/pull/4")
            .Should().BeTrue();

    [Fact]
    public void PrefixoConfuso_PedroPereiraAlvesX_Rejeita() =>
        GithubPrUrlRules.EhPermitida(
                "https://github.com/PedroPereiraAlvesX/evil/pull/1")
            .Should().BeFalse();

    [Fact]
    public void HostEvil_Rejeita() =>
        GithubPrUrlRules.EhPermitida(
                "https://github.com.evil.com/PedroPereiraAlves/repo/pull/1")
            .Should().BeFalse();

    [Fact]
    public void Http_Rejeita() =>
        GithubPrUrlRules.EhPermitida(
                "http://github.com/PedroPereiraAlves/PORTIFOLIO/pull/1")
            .Should().BeFalse();

    [Fact]
    public void OutroOwner_Rejeita() =>
        GithubPrUrlRules.EhPermitida(
                "https://github.com/outro/repo/pull/1")
            .Should().BeFalse();

    [Fact]
    public void PortaNaoPadrao_Rejeita() =>
        GithubPrUrlRules.EhPermitida(
                "https://github.com:8443/PedroPereiraAlves/repo/pull/1")
            .Should().BeFalse();
}
