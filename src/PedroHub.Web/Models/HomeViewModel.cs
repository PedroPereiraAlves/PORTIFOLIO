using PedroHub.Application.Features.Projetos.Dtos;

namespace PedroHub.Web.Models;

public sealed class HomeViewModel
{
    public IReadOnlyList<ProjetoDto> Projetos { get; init; } = [];
    public string? Erro { get; init; }
    public bool Carregando { get; init; }
}
