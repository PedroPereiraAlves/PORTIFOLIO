using System.Security.Cryptography;
using System.Text;

namespace PedroHub.Infrastructure.Escritorio;

public static class ConstantTimeEquals
{
    /// <summary>
    /// Compara strings UTF-8 em tempo constante quando os tamanhos coincidem.
    /// Tamanhos diferentes: faz trabalho fixo e retorna false sem vazar qual lado.
    /// </summary>
    public static bool Utf8(string? provided, string? expected)
    {
        var a = Encoding.UTF8.GetBytes(provided ?? string.Empty);
        var b = Encoding.UTF8.GetBytes(expected ?? string.Empty);

        if (a.Length != b.Length)
        {
            CryptographicOperations.FixedTimeEquals(b, b);
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(a, b);
    }
}
