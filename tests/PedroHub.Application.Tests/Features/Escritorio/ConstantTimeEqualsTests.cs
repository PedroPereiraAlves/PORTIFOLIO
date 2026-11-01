using FluentAssertions;
using PedroHub.Infrastructure.Escritorio;

namespace PedroHub.Application.Tests.Features.Escritorio;

public sealed class ConstantTimeEqualsTests
{
    [Fact]
    public void Utf8_Iguais_RetornaTrue()
    {
        ConstantTimeEquals.Utf8("chave-fake-dev", "chave-fake-dev").Should().BeTrue();
    }

    [Fact]
    public void Utf8_DiferentesMesmoTamanho_RetornaFalse()
    {
        ConstantTimeEquals.Utf8("chave-fake-dev", "chave-fake-dex").Should().BeFalse();
    }

    [Fact]
    public void Utf8_TamanhosDiferentes_RetornaFalse()
    {
        ConstantTimeEquals.Utf8("abc", "abcd").Should().BeFalse();
        ConstantTimeEquals.Utf8(null, "x").Should().BeFalse();
        ConstantTimeEquals.Utf8("", "x").Should().BeFalse();
    }
}
