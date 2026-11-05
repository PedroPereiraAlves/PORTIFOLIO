using System.Globalization;
using FluentAssertions;
using PedroHub.Domain.CursorConsumo;

namespace PedroHub.Application.Tests.Features.CursorConsumo;

public sealed class LeitorCsvUsoCursorTests
{
    private const string CabecalhoOficial =
        "Date,User,Cloud Agent ID,Automation ID,Kind,Model,Max Mode,Input (w/ Cache Write),Input (w/o Cache Write),Cache Read,Output Tokens,Total Tokens,Cost";

    [Fact]
    public void CabecalhoOficial_SeparaTokensECusto()
    {
        var csv = CabecalhoOficial + "\n" +
                  "2026-08-26T09:00:12.345Z,pedro@exemplo.com,,,Included,cursor-grok-4.6,No,10,20,30,40,100,-\n" +
                  "2026-08-26T10:00:00.000Z,pedro@exemplo.com,,,Usage-based,\"claude, opus\",Yes,0,1000,0,200,1200,$1.50\n" +
                  "2026-08-27T08:00:00.000Z,,,,Free,composer,No,0,0,0,0,50,Free\n";

        var leitura = LeitorCsvUsoCursor.Ler(csv);

        leitura.Sucesso.Should().BeTrue();
        leitura.Eventos.Should().HaveCount(3);
        leitura.Ignorados.Should().Be(0);

        var incluido = leitura.Eventos[0];
        incluido.OcorridoEmUtc.Should().Be(new DateTime(2026, 8, 26, 9, 0, 12, 345, DateTimeKind.Utc));
        incluido.Email.Should().Be("pedro@exemplo.com");
        incluido.Modelo.Should().Be("cursor-grok-4.6");
        incluido.Tipo.Should().Be("Included");
        incluido.ModoMaximo.Should().BeFalse();
        incluido.TokensCacheEscrita.Should().Be(10);
        incluido.TokensEntrada.Should().Be(20);
        incluido.TokensCacheLeitura.Should().Be(30);
        incluido.TokensSaida.Should().Be(40);
        incluido.TokensTotal.Should().Be(100);
        incluido.CustoCentavos.Should().BeNull();

        var cobrado = leitura.Eventos[1];
        cobrado.Modelo.Should().Be("claude, opus");
        cobrado.ModoMaximo.Should().BeTrue();
        cobrado.CustoCentavos.Should().Be(150m);

        leitura.Eventos[2].CustoCentavos.Should().Be(0);
        leitura.Eventos[2].Email.Should().BeNull();
        leitura.Eventos[2].TokensTotal.Should().Be(50);
    }

    [Fact]
    public void SemColunaCost_AceitaSoTokens()
    {
        var csv = "Data,Modelo,Input Tokens,Output Tokens\n2026-11-01T00:00:00Z,grok,10,5\n";

        var leitura = LeitorCsvUsoCursor.Ler(csv);

        leitura.Sucesso.Should().BeTrue();
        leitura.Eventos.Should().ContainSingle();
        leitura.Eventos[0].TokensTotal.Should().Be(15);
        leitura.Eventos[0].CustoCentavos.Should().BeNull();
        leitura.Eventos[0].Modelo.Should().Be("grok");
    }

    [Fact]
    public void ChargedCents_NaoMultiplicaPorCem()
    {
        var csv = "Date,Model,Total Tokens,chargedCents\n1750979225854,claude,100,21.36232\n";

        var leitura = LeitorCsvUsoCursor.Ler(csv);

        leitura.Sucesso.Should().BeTrue();
        leitura.Eventos[0].CustoCentavos.Should().Be(21.36232m);
        leitura.Eventos[0].OcorridoEmUtc.Should().Be(
            DateTimeOffset.FromUnixTimeMilliseconds(1750979225854).UtcDateTime);
    }

    [Fact]
    public void SemColunaDate_Falha()
    {
        var leitura = LeitorCsvUsoCursor.Ler("Model,Total Tokens\ngrok,10\n");

        leitura.Sucesso.Should().BeFalse();
        leitura.Erro.Should().Contain("Date");
    }

    [Fact]
    public void LinhaComDataInvalida_EIgnorada()
    {
        var csv = "Date,Model,Total Tokens,Cost\nnao-e-data,grok,10,1\n2026-11-02T00:00:00Z,grok,4,0.25\n";

        var leitura = LeitorCsvUsoCursor.Ler(csv);

        leitura.Sucesso.Should().BeTrue();
        leitura.Eventos.Should().ContainSingle();
        leitura.Ignorados.Should().Be(1);
        leitura.Avisos.Should().Contain(a => a.Contains("data inválida", StringComparison.Ordinal));
        leitura.Eventos[0].CustoCentavos.Should().Be(25m);
    }

    [Fact]
    public void Vazio_Falha()
    {
        LeitorCsvUsoCursor.Ler("   ").Sucesso.Should().BeFalse();
    }

    [Theory]
    [InlineData("Included")]
    [InlineData("incluido")]
    public void CustoIncluido_FicaSemCentavos(string custo)
    {
        var csv = $"Date,Model,Total Tokens,Cost\n2026-11-03T12:00:00Z,grok,8,{custo}\n";
        LeitorCsvUsoCursor.Ler(csv).Eventos[0].CustoCentavos.Should().BeNull();
    }
}
