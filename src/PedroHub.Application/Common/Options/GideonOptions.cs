namespace PedroHub.Application.Common.Options;

public sealed class GideonOptions
{
    public const string SectionName = "Gideon";

    /// <summary>Mapeado de HUB_GIDEON_API_KEY. Nunca logar o valor.</summary>
    public string? ApiKey { get; set; }
}