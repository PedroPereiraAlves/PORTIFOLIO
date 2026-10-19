using System.Diagnostics;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PedroHub.Application.Features.Projetos.Queries;
using PedroHub.Web.Models;

namespace PedroHub.Web.Controllers;

[AllowAnonymous]
public class HomeController : Controller
{
    private readonly IMediator _mediator;

    public HomeController(IMediator mediator) => _mediator = mediator;

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ListarProjetosPublicosQuery(), cancellationToken);

        var model = result.IsSuccess
            ? new HomeViewModel { Projetos = result.Value }
            : new HomeViewModel { Erro = result.Error };

        return View(model);
    }

    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
    }
}
