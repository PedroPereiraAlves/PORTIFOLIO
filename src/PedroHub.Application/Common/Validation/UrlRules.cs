namespace PedroHub.Application.Common.Validation;

public static class UrlRules
{
    public static bool EhHttpOuHttps(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
               && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    public static bool EhHttpOuHttpsOpcional(string? url) =>
        string.IsNullOrWhiteSpace(url) || EhHttpOuHttps(url);
}
