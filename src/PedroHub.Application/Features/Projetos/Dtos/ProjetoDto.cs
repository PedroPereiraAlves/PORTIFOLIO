using PedroHub.Domain.Enums;

namespace PedroHub.Application.Features.Projetos.Dtos;

public sealed record ProjetoDto(
    Guid Id,
    string Nome,
    string? Descricao,
    string UrlRepositorio,
    string? UrlDemo,
    string? Linguagem,
    int Stars,
    string? Topicos,
    bool Visivel,
    bool Destaque,
    int Ordem,
    OrigemProjeto Origem,
    DateTime? SincronizadoEm);
