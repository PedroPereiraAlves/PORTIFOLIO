using FluentAssertions;
using FluentValidation.TestHelper;
using PedroHub.Application.Features.Escritorio.Commands;

namespace PedroHub.Application.Tests.Features.Escritorio;

public sealed class IngerirStatusAgentesCommandValidatorTests
{
    private readonly IngerirStatusAgentesCommandValidator _sut = new();

    [Fact]
    public void StatusInvalido_Falha()
    {
        var cmd = new IngerirStatusAgentesCommand(
        [
            new StatusAgenteIngestaoItem("front-hub", "voando", null, "Hub", null, DateTime.UtcNow)
        ]);

        _sut.TestValidate(cmd).ShouldHaveValidationErrorFor("Agentes[0].Status");
    }

    [Fact]
    public void PrUrlForaDoGithub_Falha()
    {
        var cmd = new IngerirStatusAgentesCommand(
        [
            new StatusAgenteIngestaoItem(
                "front-hub", "trabalhando", "x", "Hub",
                "https://github.com/outro/repo/pull/1", DateTime.UtcNow)
        ]);

        _sut.TestValidate(cmd).ShouldHaveValidationErrorFor("Agentes[0].PrUrl");
    }

    [Fact]
    public void PayloadValido_Ok()
    {
        var cmd = new IngerirStatusAgentesCommand(
        [
            new StatusAgenteIngestaoItem(
                "front-hub", "trabalhando", "Ajustes", "Hub",
                "https://github.com/PedroPereiraAlves/PORTIFOLIO/pull/4",
                DateTime.UtcNow)
        ]);

        _sut.TestValidate(cmd).ShouldNotHaveAnyValidationErrors();
    }
}
