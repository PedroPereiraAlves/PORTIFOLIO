using PedroHub.Domain.Enums;

namespace PedroHub.Domain.Escritorio;

public static class StatusAgenteParser
{
    public static bool TentarParse(string? valor, out StatusAgenteEscritorio status)
    {
        status = StatusAgenteEscritorio.Ocioso;
        if (string.IsNullOrWhiteSpace(valor))
            return false;

        switch (valor.Trim().ToLowerInvariant())
        {
            case "ocioso":
                status = StatusAgenteEscritorio.Ocioso;
                return true;
            case "trabalhando":
                status = StatusAgenteEscritorio.Trabalhando;
                return true;
            case "revisando":
                status = StatusAgenteEscritorio.Revisando;
                return true;
            case "bloqueado":
                status = StatusAgenteEscritorio.Bloqueado;
                return true;
            case "pausado-sem-cota":
                status = StatusAgenteEscritorio.PausadoSemCota;
                return true;
            default:
                return false;
        }
    }

    public static string ParaWire(StatusAgenteEscritorio status) => status switch
    {
        StatusAgenteEscritorio.Ocioso => "ocioso",
        StatusAgenteEscritorio.Trabalhando => "trabalhando",
        StatusAgenteEscritorio.Revisando => "revisando",
        StatusAgenteEscritorio.Bloqueado => "bloqueado",
        StatusAgenteEscritorio.PausadoSemCota => "pausado-sem-cota",
        _ => "ocioso"
    };
}
