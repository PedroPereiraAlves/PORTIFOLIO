using FluentValidation;
using MediatR;
using PedroHub.Application.Common.Interfaces;
using PedroHub.Shared.Results;

namespace PedroHub.Application.Features.Auth.Commands;

public sealed record LoginOwnerCommand(string Email, string Senha, bool Lembrar)
    : IRequest<Result>;

public sealed class LoginOwnerCommandValidator : AbstractValidator<LoginOwnerCommand>
{
    public LoginOwnerCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("E-mail inválido.");
        RuleFor(x => x.Senha).NotEmpty().MinimumLength(8).WithMessage("Senha deve ter ao menos 8 caracteres.");
    }
}

public sealed class LoginOwnerCommandHandler : IRequestHandler<LoginOwnerCommand, Result>
{
    private readonly IOwnerAuthService _auth;

    public LoginOwnerCommandHandler(IOwnerAuthService auth) => _auth = auth;

    public async Task<Result> Handle(LoginOwnerCommand request, CancellationToken cancellationToken)
    {
        var (sucesso, erro) = await _auth.LoginAsync(
            request.Email,
            request.Senha,
            request.Lembrar,
            cancellationToken);

        return sucesso ? Result.Success() : Result.Failure(erro ?? "Falha no login.");
    }
}
