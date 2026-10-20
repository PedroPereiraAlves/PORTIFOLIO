using FluentAssertions;
using FluentValidation.TestHelper;
using PedroHub.Application.Features.Monitoramento.Commands;
using PedroHub.Domain.Enums;

namespace PedroHub.Application.Tests.Features.Monitoramento;

public sealed class CriarAlvoMonitoradoCommandValidatorTests
{
    private readonly CriarAlvoMonitoradoCommandValidator _validator = new();

    private static CriarAlvoMonitoradoCommand Valido() => new(
        "SITE-RMS",
        "https://site-rms-logistica.pages.dev/",
        MetodoChecagem.Get,
        300,
        10_000,
        "200",
        1500,
        true,
        null,
        "Cloudflare Pages");

    [Fact]
    public void Valido_SemErros()
    {
        var result = _validator.TestValidate(Valido());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void IntervaloBaixo_Erro()
    {
        var cmd = Valido() with { IntervaloSegundos = 30 };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.IntervaloSegundos);
    }

    [Fact]
    public void StatusEsperadoInvalido_Erro()
    {
        var cmd = Valido() with { StatusEsperado = "abc" };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.StatusEsperado);
    }

    [Fact]
    public void FaixaStatusValida_Ok()
    {
        var cmd = Valido() with { StatusEsperado = "200-399" };
        var result = _validator.TestValidate(cmd);
        result.ShouldNotHaveValidationErrorFor(x => x.StatusEsperado);
    }

    [Fact]
    public void NomeVazio_Erro()
    {
        var cmd = Valido() with { Nome = "" };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Nome);
    }

    [Fact]
    public void TimeoutForaDaFaixa_Erro()
    {
        var cmd = Valido() with { TimeoutMs = 500 };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.TimeoutMs);
    }
}
