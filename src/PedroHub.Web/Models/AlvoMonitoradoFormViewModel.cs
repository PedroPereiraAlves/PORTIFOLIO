using System.ComponentModel.DataAnnotations;
using PedroHub.Domain.Enums;

namespace PedroHub.Web.Models;

public sealed class AlvoMonitoradoFormViewModel
{
    public Guid? Id { get; set; }

    [Required, MaxLength(200), Display(Name = "Nome")]
    public string Nome { get; set; } = string.Empty;

    [Required, MaxLength(500), Display(Name = "URL")]
    public string Url { get; set; } = string.Empty;

    [Display(Name = "Método")]
    public MetodoChecagem Metodo { get; set; } = MetodoChecagem.Get;

    [Range(60, 86400), Display(Name = "Intervalo (segundos)")]
    public int IntervaloSegundos { get; set; } = 300;

    [Range(1000, 30000), Display(Name = "Timeout (ms)")]
    public int TimeoutMs { get; set; } = 10_000;

    [Required, MaxLength(20), Display(Name = "Status esperado")]
    public string StatusEsperado { get; set; } = "200";

    [Range(100, 30000), Display(Name = "Limiar degradado (ms)")]
    public int LimiarDegradadoMs { get; set; } = 1500;

    [Display(Name = "Ativo")]
    public bool Ativo { get; set; } = true;

    [Display(Name = "Projeto")]
    public Guid? ProjetoId { get; set; }

    [MaxLength(100), Display(Name = "Infra")]
    public string? Infra { get; set; }

    public bool EhEdicao => Id.HasValue;
}
