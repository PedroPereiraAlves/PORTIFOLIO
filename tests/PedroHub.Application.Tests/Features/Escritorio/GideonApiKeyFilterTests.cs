using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PedroHub.Application.Common.Options;
using PedroHub.Web.Security;

namespace PedroHub.Application.Tests.Features.Escritorio;

public sealed class GideonApiKeyFilterTests
{
    private static ActionExecutingContext CriarContexto(string? headerValue)
    {
        var http = new DefaultHttpContext();
        if (headerValue is not null)
            http.Request.Headers[GideonApiKeyFilter.HeaderName] = headerValue;

        var actionContext = new ActionContext(http, new RouteData(), new ActionDescriptor());
        return new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object?>(),
            controller: new object());
    }

    [Fact]
    public async Task SemChaveConfigurada_503()
    {
        var filter = new GideonApiKeyFilter(
            Options.Create(new GideonOptions { ApiKey = null }),
            NullLogger<GideonApiKeyFilter>.Instance);
        var ctx = CriarContexto("qualquer");
        var called = false;

        await filter.OnActionExecutionAsync(ctx, () =>
        {
            called = true;
            return Task.FromResult(new ActionExecutedContext(ctx, new List<IFilterMetadata>(), new object()));
        });

        called.Should().BeFalse();
        ctx.Result.Should().BeOfType<StatusCodeResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
    }

    [Fact]
    public async Task SemHeader_401()
    {
        var filter = new GideonApiKeyFilter(
            Options.Create(new GideonOptions { ApiKey = "chave-fake-dev" }),
            NullLogger<GideonApiKeyFilter>.Instance);
        var ctx = CriarContexto(null);
        await filter.OnActionExecutionAsync(ctx, () =>
            Task.FromResult(new ActionExecutedContext(ctx, new List<IFilterMetadata>(), new object())));

        ctx.Result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task ChaveErrada_401()
    {
        var filter = new GideonApiKeyFilter(
            Options.Create(new GideonOptions { ApiKey = "chave-fake-dev" }),
            NullLogger<GideonApiKeyFilter>.Instance);
        var ctx = CriarContexto("errada");
        await filter.OnActionExecutionAsync(ctx, () =>
            Task.FromResult(new ActionExecutedContext(ctx, new List<IFilterMetadata>(), new object())));

        ctx.Result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task ChaveCerta_Segue()
    {
        var filter = new GideonApiKeyFilter(
            Options.Create(new GideonOptions { ApiKey = "chave-fake-dev" }),
            NullLogger<GideonApiKeyFilter>.Instance);
        var ctx = CriarContexto("chave-fake-dev");
        var called = false;

        await filter.OnActionExecutionAsync(ctx, () =>
        {
            called = true;
            return Task.FromResult(new ActionExecutedContext(ctx, new List<IFilterMetadata>(), new object()));
        });

        called.Should().BeTrue();
        ctx.Result.Should().BeNull();
    }
}
