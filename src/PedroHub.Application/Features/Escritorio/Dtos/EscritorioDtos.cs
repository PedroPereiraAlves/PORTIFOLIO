namespace PedroHub.Application.Features.Escritorio.Dtos;

public sealed record EventoAgenteDto(
    string Tipo,
    string Titulo,
    string? Descricao,
    string? Link,
    DateTime OcorridoEm);

public sealed record AgenteEstadoDto(
    string Slug,
    string Nome,
    string Papel,
    string Sala,
    string CorAvatar,
    string Status,
    string? TarefaAtual,
    string? Projeto,
    string? PrUrl,
    DateTime? AtualizadoEm,
    string HaQuantoTempo,
    bool Desatualizado,
    IReadOnlyList<EventoAgenteDto> UltimasEntregas);

public sealed record EscritorioEstadoDto(
    int PollingSegundos,
    IReadOnlyList<AgenteEstadoDto> Agentes);

public sealed record AgenteAdminDto(
    Guid Id,
    string Slug,
    string Nome,
    string Papel,
    string Sala,
    int Ordem,
    bool Ativo);
