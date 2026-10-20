using PedroHub.Domain.Enums;

namespace PedroHub.Domain.Monitoramento;

public static class RegraStatusAlvo
{
    public sealed record Entrada(
        ResultadoChecagemHttp Resultado,
        string StatusEsperado,
        int LimiarDegradadoMs,
        int FalhasConsecutivasAtuais,
        StatusAlvo StatusAnterior,
        int FalhasParaOffline = 2);

    public sealed record Saida(
        StatusAlvo NovoStatus,
        int NovasFalhasConsecutivas,
        bool StatusMudou);

    public static Saida Avaliar(Entrada entrada)
    {
        var falhasParaOffline = Math.Max(2, entrada.FalhasParaOffline);
        var resultado = entrada.Resultado;

        if (!resultado.TimeoutOuErroRede
            && resultado.SucessoHttp
            && StatusEsperadoParser.Corresponde(entrada.StatusEsperado, resultado.CodigoHttp))
        {
            var status = resultado.LatenciaMs <= entrada.LimiarDegradadoMs
                ? StatusAlvo.Online
                : StatusAlvo.Degradado;

            return new Saida(
                status,
                NovasFalhasConsecutivas: 0,
                StatusMudou: status != entrada.StatusAnterior);
        }

        var falhas = entrada.FalhasConsecutivasAtuais + 1;
        var novo = falhas >= falhasParaOffline ? StatusAlvo.Offline : StatusAlvo.Degradado;

        return new Saida(
            novo,
            NovasFalhasConsecutivas: falhas,
            StatusMudou: novo != entrada.StatusAnterior);
    }
}
