using PedroHub.Domain.Enums;

namespace PedroHub.Application.Features.Monitoramento.Dtos;

public sealed record AlvoMonitoradoDto(
    Guid Id,
    string Nome,
    string Url,
    MetodoChecagem Metodo,
    int IntervaloSegundos,
    int TimeoutMs,
    string StatusEsperado,
    int LimiarDegradadoMs,
    bool Ativo,
    Guid? ProjetoId,
    string? ProjetoNome,
    string? Infra,
    StatusAlvo StatusAtual,
    int FalhasConsecutivas,
    DateTime? UltimaChecagemEm,
    DateTime? ProximaChecagemEm,
    DateTime? UltimaMudancaStatusEm,
    int? LatenciaAtualMs,
    double Uptime24h,
    double Uptime7d,
    double Uptime30d,
    int P50Ms,
    int P95Ms);

public sealed record ChecagemDto(
    Guid Id,
    DateTime ExecutadaEm,
    StatusAlvo Status,
    int LatenciaMs,
    int? CodigoHttp,
    string? Erro);

public sealed record PontoLatenciaDto(DateTime Em, int LatenciaMs, StatusAlvo Status);

public sealed record ResumoMonitoramentoDto(
    IReadOnlyList<AlvoMonitoradoDto> Alvos,
    DateTime? UltimaExecucaoCheckerEm,
    DateTime? ProximaExecucaoCheckerEm,
    IReadOnlyList<PontoLatenciaDto> SerieLatencia,
    string Periodo);

public sealed record ProjetoSelectDto(Guid Id, string Nome);
