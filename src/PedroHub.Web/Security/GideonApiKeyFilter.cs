using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using PedroHub.Application.Common.Options;
using PedroHub.Infrastructure.Escritorio;

namespace PedroHub.Web.Security;

/// <summary>
/// Protege endpoints do Gideon por header X-Gideon-Key (sem cookie/antiforgery).
/// Sem chave configurada → 503; ausente/errada → 401 genérico.
/// </summary>
public sealed class GideonApiKeyFilter : IAsyncActionFilter
{
    public const string HeaderName = "X-Gideon-Key";

    private readonly GideonOptions _options;
    private readonly ILogger<GideonApiKeyFilter> _logger;

    public GideonApiKeyFilter(IOptions<GideonOptions> options, ILogger<GideonApiKeyFilter> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var configurada = _options.ApiKey;
        if (string.IsNullOrWhiteSpace(configurada))
        {
            _logger.LogWarning(
                "Endpoint Gideon recusado: chave não configurada. TraceId={TraceId}",
                Activity.Current?.Id ?? context.HttpContext.TraceIdentifier);
            context.Result = new StatusCodeResult(StatusCodes.Status503ServiceUnavailable);
            return;
        }

        var headers = context.HttpContext.Request.Headers;
        var fornecida = headers.TryGetValue(HeaderName, out var valores)
            ? valores.ToString()
            : null;

        if (!ConstantTimeEquals.Utf8(fornecida, configurada))
        {
            var ip = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            _logger.LogInformation(
                "chave inválida. IP={Ip} TraceId={TraceId}",
                ip,
                Activity.Current?.Id ?? context.HttpContext.TraceIdentifier);
            context.Result = new UnauthorizedResult();
            return;
        }

        await next();
    }
}
