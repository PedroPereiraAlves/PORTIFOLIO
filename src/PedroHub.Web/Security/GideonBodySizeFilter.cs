using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace PedroHub.Web.Security;

/// <summary>
/// Recusa corpos com Content-Length &gt; 64 KB após a autenticação e antes do model binding.
/// Complementa [RequestSizeLimit] (que o TestServer nem sempre aplica como 413).
/// </summary>
public sealed class GideonBodySizeFilter : IResourceFilter
{
    public const int MaxBytes = 65_536;

    public void OnResourceExecuting(ResourceExecutingContext context)
    {
        var length = context.HttpContext.Request.ContentLength;
        if (length is > MaxBytes)
            context.Result = new StatusCodeResult(StatusCodes.Status413PayloadTooLarge);
    }

    public void OnResourceExecuted(ResourceExecutedContext context)
    {
    }
}
