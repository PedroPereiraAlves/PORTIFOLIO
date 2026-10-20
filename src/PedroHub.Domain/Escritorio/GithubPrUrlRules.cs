namespace PedroHub.Domain.Escritorio;

public static class GithubPrUrlRules
{
    private const string OwnerPermitido = "PedroPereiraAlves";

    public static bool EhPermitida(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return true;

        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
            return false;

        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            return false;

        if (!string.Equals(uri.Host, "github.com", StringComparison.OrdinalIgnoreCase))
            return false;

        if (!uri.IsDefaultPort)
            return false;

        if (!string.IsNullOrEmpty(uri.UserInfo))
            return false;

        var segmentos = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segmentos.Length == 0)
            return false;

        return string.Equals(segmentos[0], OwnerPermitido, StringComparison.OrdinalIgnoreCase);
    }
}
