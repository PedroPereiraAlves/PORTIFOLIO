namespace PedroHub.Domain.Escritorio;

public static class GithubPrUrlRules
{
    private const string Prefixo = "https://github.com/PedroPereiraAlves/";

    public static bool EhPermitida(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return true;

        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
            return false;

        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            return false;

        if (!string.IsNullOrEmpty(uri.UserInfo))
            return false;

        var absolute = uri.GetLeftPart(UriPartial.Path).TrimEnd('/');
        return absolute.StartsWith(Prefixo.TrimEnd('/'), StringComparison.OrdinalIgnoreCase)
               || absolute.StartsWith(Prefixo, StringComparison.OrdinalIgnoreCase);
    }
}
