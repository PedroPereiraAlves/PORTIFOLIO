using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using PedroHub.Application.Common.Interfaces;
using PedroHub.Application.Features.Sync.Commands;
using PedroHub.Domain.Entities;
using PedroHub.Domain.Enums;
using PedroHub.Infrastructure.Persistence;

namespace PedroHub.Application.Tests.Features.Sync;

public sealed class SincronizarStarsGitHubCommandHandlerTests
{
    private static ApplicationDbContext CriarDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task Handle_DeveCriarProjetoDoOwnerComoVisivel()
    {
        await using var db = CriarDb();
        var client = Substitute.For<IGitHubStarredClient>();
        client.ObterReposAsync(Arg.Any<CancellationToken>()).Returns(
        [
            new GitHubStarredRepo(
                "LaunchKit.NET",
                "https://github.com/PedroPereiraAlves/LaunchKit.NET",
                "Template",
                "C#",
                1,
                ["dotnet"],
                "PedroPereiraAlves")
        ]);

        var handler = new SincronizarStarsGitHubCommandHandler(
            client, db, db, NullLogger<SincronizarStarsGitHubCommandHandler>.Instance);

        var result = await handler.Handle(new SincronizarStarsGitHubCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Criados.Should().Be(1);

        var projeto = await db.Projetos.SingleAsync();
        projeto.Visivel.Should().BeTrue();
        projeto.Origem.Should().Be(OrigemProjeto.GitHub);
        projeto.Stars.Should().Be(1);
        projeto.UrlRepositorio.Should().Be("https://github.com/PedroPereiraAlves/LaunchKit.NET");
    }

    [Fact]
    public async Task Handle_DeveOcultarReposDeTerceirosPorPadrao()
    {
        await using var db = CriarDb();
        var client = Substitute.For<IGitHubStarredClient>();
        client.ObterReposAsync(Arg.Any<CancellationToken>()).Returns(
        [
            new GitHubStarredRepo(
                "aspnetcore",
                "https://github.com/dotnet/aspnetcore",
                "ASP.NET Core",
                "C#",
                999,
                ["aspnet"],
                "dotnet")
        ]);

        var handler = new SincronizarStarsGitHubCommandHandler(
            client, db, db, NullLogger<SincronizarStarsGitHubCommandHandler>.Instance);

        var result = await handler.Handle(new SincronizarStarsGitHubCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var projeto = await db.Projetos.SingleAsync();
        projeto.Visivel.Should().BeFalse();
        projeto.Nome.Should().Be("aspnetcore");
    }

    [Fact]
    public async Task Handle_DeveFazerUpsertPelaUrlDoRepositorio()
    {
        await using var db = CriarDb();
        db.Projetos.Add(new Projeto
        {
            Nome = "LaunchKit.NET",
            UrlRepositorio = "https://github.com/PedroPereiraAlves/LaunchKit.NET",
            Stars = 0,
            Visivel = true,
            Origem = OrigemProjeto.GitHub
        });
        await db.SaveChangesAsync();

        var client = Substitute.For<IGitHubStarredClient>();
        client.ObterReposAsync(Arg.Any<CancellationToken>()).Returns(
        [
            new GitHubStarredRepo(
                "LaunchKit.NET",
                "https://github.com/PedroPereiraAlves/LaunchKit.NET/",
                "Atualizado",
                "C#",
                5,
                ["dotnet", "template"],
                "PedroPereiraAlves")
        ]);

        var handler = new SincronizarStarsGitHubCommandHandler(
            client, db, db, NullLogger<SincronizarStarsGitHubCommandHandler>.Instance);

        var result = await handler.Handle(new SincronizarStarsGitHubCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Criados.Should().Be(0);
        result.Value.Atualizados.Should().Be(1);

        var projetos = await db.Projetos.ToListAsync();
        projetos.Should().HaveCount(1);
        projetos[0].Stars.Should().Be(5);
        projetos[0].Descricao.Should().Be("Atualizado");
        projetos[0].Topicos.Should().Be("dotnet,template");
        projetos[0].SincronizadoEm.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_DevePreservarDescricaoCustomizadaNoUpdate()
    {
        await using var db = CriarDb();
        db.Projetos.Add(new Projeto
        {
            Nome = "Antigo",
            Descricao = "Descrição editada no admin",
            UrlRepositorio = "https://github.com/PedroPereiraAlves/LaunchKit.NET",
            Linguagem = "F#",
            Stars = 0,
            Visivel = true,
            Origem = OrigemProjeto.GitHub
        });
        await db.SaveChangesAsync();

        var client = Substitute.For<IGitHubStarredClient>();
        client.ObterReposAsync(Arg.Any<CancellationToken>()).Returns(
        [
            new GitHubStarredRepo(
                "LaunchKit.NET",
                "https://github.com/PedroPereiraAlves/LaunchKit.NET",
                "Descrição do GitHub",
                "C#",
                12,
                ["dotnet"],
                "PedroPereiraAlves")
        ]);

        var handler = new SincronizarStarsGitHubCommandHandler(
            client, db, db, NullLogger<SincronizarStarsGitHubCommandHandler>.Instance);

        var result = await handler.Handle(new SincronizarStarsGitHubCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var projeto = await db.Projetos.SingleAsync();
        projeto.Descricao.Should().Be("Descrição editada no admin");
        projeto.Nome.Should().Be("LaunchKit.NET");
        projeto.Stars.Should().Be(12);
        projeto.Linguagem.Should().Be("C#");
    }

    [Fact]
    public async Task Handle_DeveFazerUpsertEmProjetoManualComMesmaUrl()
    {
        await using var db = CriarDb();
        db.Projetos.Add(new Projeto
        {
            Nome = "Manual",
            Descricao = "Cadastro manual",
            UrlRepositorio = "https://github.com/PedroPereiraAlves/LaunchKit.NET",
            Stars = 0,
            Visivel = true,
            Origem = OrigemProjeto.Manual
        });
        await db.SaveChangesAsync();

        var client = Substitute.For<IGitHubStarredClient>();
        client.ObterReposAsync(Arg.Any<CancellationToken>()).Returns(
        [
            new GitHubStarredRepo(
                "LaunchKit.NET",
                "https://github.com/PedroPereiraAlves/LaunchKit.NET",
                "Do GitHub",
                "C#",
                7,
                ["dotnet"],
                "PedroPereiraAlves")
        ]);

        var handler = new SincronizarStarsGitHubCommandHandler(
            client, db, db, NullLogger<SincronizarStarsGitHubCommandHandler>.Instance);

        var result = await handler.Handle(new SincronizarStarsGitHubCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Criados.Should().Be(0);
        result.Value.Atualizados.Should().Be(1);

        var projetos = await db.Projetos.ToListAsync();
        projetos.Should().HaveCount(1);
        projetos[0].Origem.Should().Be(OrigemProjeto.Manual);
        projetos[0].Descricao.Should().Be("Cadastro manual");
        projetos[0].Nome.Should().Be("LaunchKit.NET");
        projetos[0].Stars.Should().Be(7);
        projetos[0].Linguagem.Should().Be("C#");
    }
}
