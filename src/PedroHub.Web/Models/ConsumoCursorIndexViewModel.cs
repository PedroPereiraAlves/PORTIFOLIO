using PedroHub.Application.Features.CursorConsumo.Dtos;

namespace PedroHub.Web.Models;

public sealed class ConsumoCursorIndexViewModel
{
    public ConsumoCursorDto? Resumo { get; init; }
    public string DadosGraficoJson { get; init; } = "[]";
}
