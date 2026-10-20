namespace PedroHub.Domain.Monitoramento;

public sealed record ResultadoChecagemHttp(
    bool SucessoHttp,
    int? CodigoHttp,
    int LatenciaMs,
    bool TimeoutOuErroRede,
    string? Erro);
