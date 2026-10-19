namespace PedroHub.Application.Common.Options;

public sealed class MonitoramentoOptions
{
    public const string SectionName = "Monitoramento";

    public int RetencaoDias { get; set; } = 30;
    public int FalhasParaOffline { get; set; } = 2;
    public int ConcorrenciaMaxima { get; set; } = 4;
    public int[] PortasPermitidas { get; set; } = [80, 443];
    public AlertasOptions Alertas { get; set; } = new();
}

public sealed class AlertasOptions
{
    public string? De { get; set; }
    public string? Para { get; set; }
    public int MinutosEntreEmailsPorAlvo { get; set; } = 10;
    public int MaxEmailsPorHora { get; set; } = 20;
}
