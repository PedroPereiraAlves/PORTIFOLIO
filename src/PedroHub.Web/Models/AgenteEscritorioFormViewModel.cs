using System.ComponentModel.DataAnnotations;
using PedroHub.Domain.Enums;

namespace PedroHub.Web.Models;

public sealed class AgenteEscritorioFormViewModel
{
    public Guid Id { get; set; }

    [Display(Name = "Slug")]
    public string Slug { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string Nome { get; set; } = string.Empty;

    [Required, MaxLength(160)]
    public string Papel { get; set; } = string.Empty;

    [Required]
    public SalaEscritorio Sala { get; set; }

    [Range(0, 999)]
    public int Ordem { get; set; }

    public bool Ativo { get; set; } = true;
}
