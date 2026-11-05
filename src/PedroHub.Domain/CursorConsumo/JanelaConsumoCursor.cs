namespace PedroHub.Domain.CursorConsumo;

public static class JanelaConsumoCursor
{
    public const string PeriodoPadrao = "30d";

    public static string NormalizarPeriodo(string? periodo) => periodo switch
    {
        "7d" or "30d" or "90d" or "ciclo" => periodo,
        _ => PeriodoPadrao
    };

    public static (DateTime InicioUtc, DateTime FimExclusivoUtc) Resolver(
        string? periodo,
        DateTime agoraUtc,
        int diaInicioCiclo)
    {
        var agora = Utc(agoraUtc);
        var fimHoje = agora.Date.AddDays(1);
        return NormalizarPeriodo(periodo) switch
        {
            "7d" => (fimHoje.AddDays(-7), fimHoje),
            "90d" => (fimHoje.AddDays(-90), fimHoje),
            "ciclo" => Ciclo(agora, diaInicioCiclo),
            _ => (fimHoje.AddDays(-30), fimHoje)
        };
    }

    public static (DateTime InicioUtc, DateTime FimExclusivoUtc) Ciclo(DateTime agoraUtc, int diaInicioCiclo)
    {
        var agora = Utc(agoraUtc);
        var dia = Math.Clamp(diaInicioCiclo, 1, 28);
        var inicio = new DateTime(agora.Year, agora.Month, dia, 0, 0, 0, DateTimeKind.Utc);
        if (agora < inicio)
            inicio = inicio.AddMonths(-1);
        return (inicio, inicio.AddMonths(1));
    }

    private static DateTime Utc(DateTime valor) =>
        valor.Kind == DateTimeKind.Utc ? valor : DateTime.SpecifyKind(valor, DateTimeKind.Utc);
}
