using PedroHub.Application.Features.Monitoramento.Dtos;
using PedroHub.Application.Features.Monitoramento.Queries;

namespace PedroHub.Web.Models;

public sealed class MonitoramentoIndexViewModel
{
    public required ResumoMonitoramentoDto Resumo { get; init; }
    public Guid? AlvoHistoricoId { get; init; }
    public ListaChecagensDto? Historico { get; init; }
    public string DadosLatenciaJson { get; init; } = "[]";
}
