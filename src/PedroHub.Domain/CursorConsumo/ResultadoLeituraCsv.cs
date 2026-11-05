namespace PedroHub.Domain.CursorConsumo;

public sealed record ResultadoLeituraCsv(
    bool Sucesso,
    string? Erro,
    IReadOnlyList<EventoUsoNormalizado> Eventos,
    IReadOnlyList<string> Avisos,
    int LinhasDados,
    int Ignorados)
{
    public static ResultadoLeituraCsv Falha(string erro) =>
        new(false, erro, [], [], 0, 0);
}
