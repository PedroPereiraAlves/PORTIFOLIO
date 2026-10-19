using System.ComponentModel.DataAnnotations;

namespace PedroHub.Web.Models;

public sealed class ProjetoFormViewModel
{
    public Guid? Id { get; set; }

    [Required]
    [MaxLength(200)]
    [Display(Name = "Nome")]
    public string Nome { get; set; } = string.Empty;

    [MaxLength(4000)]
    [Display(Name = "Descrição")]
    public string? Descricao { get; set; }

    [Required]
    [MaxLength(500)]
    [Url]
    [Display(Name = "URL do repositório")]
    public string UrlRepositorio { get; set; } = string.Empty;

    [MaxLength(500)]
    [Url]
    [Display(Name = "URL demo")]
    public string? UrlDemo { get; set; }

    [MaxLength(100)]
    [Display(Name = "Linguagem")]
    public string? Linguagem { get; set; }

    [Display(Name = "Visível")]
    public bool Visivel { get; set; } = true;

    [Display(Name = "Destaque")]
    public bool Destaque { get; set; }

    [Range(0, int.MaxValue)]
    [Display(Name = "Ordem")]
    public int Ordem { get; set; }

    public bool EhEdicao => Id.HasValue;
}
