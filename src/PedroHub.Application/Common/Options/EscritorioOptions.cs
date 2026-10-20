namespace PedroHub.Application.Common.Options;

public sealed class EscritorioOptions
{
    public const string SectionName = "Escritorio";

    public int RetencaoDias { get; set; } = 90;
    public int MinutosDesatualizado { get; set; } = 30;
    public int PollingSegundos { get; set; } = 20;
}
