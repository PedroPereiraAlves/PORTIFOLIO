using PedroHub.Domain.Enums;

namespace PedroHub.Domain.Escritorio;

public static class TipoEventoAgenteParser
{
    public static bool TentarParse(string? valor, out TipoEventoAgente tipo)
    {
        tipo = TipoEventoAgente.Entrega;
        if (string.IsNullOrWhiteSpace(valor))
            return false;

        switch (valor.Trim().ToLowerInvariant())
        {
            case "entrega":
                tipo = TipoEventoAgente.Entrega;
                return true;
            case "pr-aberto":
                tipo = TipoEventoAgente.PrAberto;
                return true;
            case "pr-mergeado":
                tipo = TipoEventoAgente.PrMergeado;
                return true;
            case "auditoria":
                tipo = TipoEventoAgente.Auditoria;
                return true;
            case "consolidacao":
                tipo = TipoEventoAgente.Consolidacao;
                return true;
            case "bloqueio":
                tipo = TipoEventoAgente.Bloqueio;
                return true;
            default:
                return false;
        }
    }

    public static string ParaWire(TipoEventoAgente tipo) => tipo switch
    {
        TipoEventoAgente.Entrega => "entrega",
        TipoEventoAgente.PrAberto => "pr-aberto",
        TipoEventoAgente.PrMergeado => "pr-mergeado",
        TipoEventoAgente.Auditoria => "auditoria",
        TipoEventoAgente.Consolidacao => "consolidacao",
        TipoEventoAgente.Bloqueio => "bloqueio",
        _ => "entrega"
    };
}
