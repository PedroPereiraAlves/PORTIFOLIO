using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace PedroHub.Web.Security;

/// <summary>
/// Limita o corpo a 64 KB após a autenticação e antes do model binding.
/// Conta bytes lidos (cobre chunked sem Content-Length) e define MaxRequestBodySize.
/// </summary>
public sealed class GideonBodySizeFilter : IAsyncResourceFilter
{
    public const int MaxBytes = 65_536;

    public async Task OnResourceExecutionAsync(
        ResourceExecutingContext context,
        ResourceExecutionDelegate next)
    {
        var feature = context.HttpContext.Features.Get<IHttpMaxRequestBodySizeFeature>();
        if (feature is { IsReadOnly: false })
            feature.MaxRequestBodySize = MaxBytes;

        if (context.HttpContext.Request.ContentLength is > MaxBytes)
        {
            context.Result = new StatusCodeResult(StatusCodes.Status413PayloadTooLarge);
            return;
        }

        // Buffer com teto: chunked ou Content-Length omitido ainda respeitam o limite.
        var original = context.HttpContext.Request.Body;
        var limited = new MemoryStream();
        var buffer = new byte[8192];
        long total = 0;
        while (true)
        {
            var read = await original.ReadAsync(buffer.AsMemory(0, buffer.Length));
            if (read <= 0)
                break;
            total += read;
            if (total > MaxBytes)
            {
                context.Result = new StatusCodeResult(StatusCodes.Status413PayloadTooLarge);
                return;
            }

            await limited.WriteAsync(buffer.AsMemory(0, read));
        }

        limited.Position = 0;
        context.HttpContext.Request.Body = limited;
        await next();
    }
}
