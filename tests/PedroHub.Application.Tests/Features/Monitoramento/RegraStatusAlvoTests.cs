using FluentAssertions;
using PedroHub.Domain.Enums;
using PedroHub.Domain.Monitoramento;

namespace PedroHub.Application.Tests.Features.Monitoramento;

public sealed class RegraStatusAlvoTests
{
    private static ResultadoChecagemHttp Ok(int codigo, int latencia) =>
        new(true, codigo, latencia, false, null);

    private static ResultadoChecagemHttp Falha(string? erro = "erro") =>
        new(false, null, 0, true, erro);

    [Fact]
    public void Avaliar_StatusEsperadoELatenciaBaixa_OnlineEZeraFalhas()
    {
        var saida = RegraStatusAlvo.Avaliar(new(
            Ok(200, 100),
            "200",
            LimiarDegradadoMs: 1500,
            FalhasConsecutivasAtuais: 3,
            StatusAnterior: StatusAlvo.Offline));

        saida.NovoStatus.Should().Be(StatusAlvo.Online);
        saida.NovasFalhasConsecutivas.Should().Be(0);
        saida.StatusMudou.Should().BeTrue();
    }

    [Fact]
    public void Avaliar_StatusEsperadoELatenciaAlta_DegradadoEZeraFalhas()
    {
        var saida = RegraStatusAlvo.Avaliar(new(
            Ok(200, 2000),
            "200",
            LimiarDegradadoMs: 1500,
            FalhasConsecutivasAtuais: 1,
            StatusAnterior: StatusAlvo.Online));

        saida.NovoStatus.Should().Be(StatusAlvo.Degradado);
        saida.NovasFalhasConsecutivas.Should().Be(0);
    }

    [Fact]
    public void Avaliar_UmaFalha_Degradado()
    {
        var saida = RegraStatusAlvo.Avaliar(new(
            Falha("timeout"),
            "200",
            1500,
            FalhasConsecutivasAtuais: 0,
            StatusAnterior: StatusAlvo.Online,
            FalhasParaOffline: 2));

        saida.NovoStatus.Should().Be(StatusAlvo.Degradado);
        saida.NovasFalhasConsecutivas.Should().Be(1);
    }

    [Fact]
    public void Avaliar_DuasFalhas_Offline()
    {
        var saida = RegraStatusAlvo.Avaliar(new(
            Falha(),
            "200",
            1500,
            FalhasConsecutivasAtuais: 1,
            StatusAnterior: StatusAlvo.Degradado,
            FalhasParaOffline: 2));

        saida.NovoStatus.Should().Be(StatusAlvo.Offline);
        saida.NovasFalhasConsecutivas.Should().Be(2);
    }

    [Fact]
    public void Avaliar_Recuperacao_ZeraFalhas()
    {
        var saida = RegraStatusAlvo.Avaliar(new(
            Ok(200, 50),
            "200-399",
            1500,
            FalhasConsecutivasAtuais: 5,
            StatusAnterior: StatusAlvo.Offline));

        saida.NovoStatus.Should().Be(StatusAlvo.Online);
        saida.NovasFalhasConsecutivas.Should().Be(0);
    }
}
