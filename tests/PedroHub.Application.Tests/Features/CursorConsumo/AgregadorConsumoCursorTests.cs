using FluentAssertions;
using PedroHub.Domain.CursorConsumo;

namespace PedroHub.Application.Tests.Features.CursorConsumo;

public sealed class AgregadorConsumoCursorTests
{
    private static readonly DateTime Agora = new(2026, 11, 5, 15, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void AgrupaPorDiaEModelo_ECalculaCota()
    {
        var eventos = new[]
        {
            Evento(new DateTime(2026, 11, 4, 10, 0, 0, DateTimeKind.Utc), "grok", 1000, 200m),
            Evento(new DateTime(2026, 11, 4, 11, 0, 0, DateTimeKind.Utc), "opus", 500, null),
            Evento(new DateTime(2026, 11, 5, 9, 0, 0, DateTimeKind.Utc), "grok", 1500, 100m),
            Evento(new DateTime(2026, 10, 20, 9, 0, 0, DateTimeKind.Utc), "grok", 9000, 50m)
        };

        var periodo = JanelaConsumoCursor.Resolver("7d", Agora, 1);
        var ciclo = JanelaConsumoCursor.Ciclo(Agora, 1);
        var agregado = AgregadorConsumoCursor.Agregar(
            eventos, periodo.InicioUtc, periodo.FimExclusivoUtc, ciclo.InicioUtc, ciclo.FimExclusivoUtc, 10_000, 20m);

        agregado.Requisicoes.Should().Be(3);
        agregado.Tokens.Should().Be(3000);
        agregado.CustoCentavos.Should().Be(300m);
        agregado.CustoIncompleto.Should().BeTrue();
        agregado.TokensCiclo.Should().Be(3000);
        agregado.PercentualCotaTokens.Should().Be(30.0m);
        agregado.PercentualCotaUsd.Should().Be(15.0m);

        agregado.PorModelo.Select(m => m.Modelo).Should().Equal("grok", "opus");
        agregado.PorModelo[0].Tokens.Should().Be(2500);
        agregado.PorModelo[0].PercentualTokens.Should().Be(83.3m);

        var dia4 = agregado.PorDia.Single(d => d.Dia == new DateOnly(2026, 11, 4));
        dia4.Requisicoes.Should().Be(2);
        dia4.Tokens.Should().Be(1500);
        agregado.PorDia.Should().OnlyContain(d => d.Dia >= new DateOnly(2026, 10, 30) && d.Dia <= new DateOnly(2026, 11, 5));
        agregado.PorDia.Single(d => d.Dia == new DateOnly(2026, 11, 3)).Tokens.Should().Be(0);
    }

    [Fact]
    public void CotaAusente_PercentualFicaNulo()
    {
        var agregado = AgregadorConsumoCursor.Agregar(
            [],
            Agora.Date.AddDays(-6),
            Agora.Date.AddDays(1),
            new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc),
            null,
            null);

        agregado.PercentualCotaTokens.Should().BeNull();
        agregado.PercentualCotaUsd.Should().BeNull();
        agregado.CustoCentavos.Should().Be(0);
        agregado.Requisicoes.Should().Be(0);
    }

    [Fact]
    public void VinteDolaresDeCota_DoisMilCentavosSaoCemPorCento()
    {
        AgregadorConsumoCursor.PercentualUsd(2000m, 20m).Should().Be(100m);
    }

    [Fact]
    public void Ciclo_AntesDoDiaDeInicio_UsaMesAnterior()
    {
        var agora = new DateTime(2026, 11, 5, 0, 0, 0, DateTimeKind.Utc);
        var (inicio, fim) = JanelaConsumoCursor.Ciclo(agora, 12);

        inicio.Should().Be(new DateTime(2026, 10, 12, 0, 0, 0, DateTimeKind.Utc));
        fim.Should().Be(new DateTime(2026, 11, 12, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void Ciclo_NoDiaDeInicio_ComecaHoje()
    {
        var agora = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);
        var (inicio, fim) = JanelaConsumoCursor.Ciclo(agora, 1);

        inicio.Should().Be(agora);
        fim.Should().Be(new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void DiaDeCicloAcimaDe28_ELimitado()
    {
        var agora = new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc);
        var (inicio, _) = JanelaConsumoCursor.Ciclo(agora, 31);
        inicio.Day.Should().Be(28);
        inicio.Month.Should().Be(2);
    }

    [Fact]
    public void Chave_EEstavelEMudaComOCusto()
    {
        var a = Evento(Agora, "grok", 10, 1m);
        var b = Evento(Agora, "grok", 10, 2m);
        ChaveEventoUsoCursor.Calcular(a).Should().Be(ChaveEventoUsoCursor.Calcular(a));
        ChaveEventoUsoCursor.Calcular(a).Should().NotBe(ChaveEventoUsoCursor.Calcular(b));
        ChaveEventoUsoCursor.Calcular(a).Should().HaveLength(64);
    }

    private static EventoUsoNormalizado Evento(DateTime quando, string modelo, long tokens, decimal? centavos) =>
        new(quando, "pedro@exemplo.com", modelo, "Included", false, tokens, 0, 0, 0, tokens, centavos, 1m);
}
