using FluentAssertions;
using PedroHub.Domain.Enums;
using PedroHub.Domain.Monitoramento;

namespace PedroHub.Application.Tests.Features.Monitoramento;

public sealed class MetricasMonitoramentoTests
{
    [Fact]
    public void CalcularUptime_DegradadoContaComoUp()
    {
        var statuses = new[]
        {
            StatusAlvo.Online,
            StatusAlvo.Degradado,
            StatusAlvo.Offline,
            StatusAlvo.Online
        };

        MetricasMonitoramento.CalcularUptime(statuses).Should().Be(0.75);
    }

    [Fact]
    public void CalcularUptime_Vazio_Zero()
    {
        MetricasMonitoramento.CalcularUptime([]).Should().Be(0);
    }

    [Fact]
    public void CalcularPercentisLatencia_DadosConhecidos()
    {
        // 10 valores: p50 ~ índice 4.5 → média 5 e 6 = 150; p95 ~ índice 8.55
        var latencias = new[] { 100, 110, 120, 130, 140, 150, 160, 170, 180, 200 };

        var (p50, p95) = MetricasMonitoramento.CalcularPercentisLatencia(latencias);

        p50.Should().Be(145);
        p95.Should().Be(191);
    }
}
