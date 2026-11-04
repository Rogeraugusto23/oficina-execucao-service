using ExecucaoService.Domain.Entities;
using ExecucaoService.Domain.Enums;
using FluentAssertions;

namespace ExecucaoService.Tests;

public class ExecucaoTests
{
    [Fact]
    public void CriarExecucao_DeveIniciarNaFila()
    {
        var execucao = new Execucao(Guid.NewGuid(), "202611031200");
        execucao.Status.Should().Be(StatusExecucao.NaFila);
    }

    [Fact]
    public void Iniciar_QuandoNaFila_DeveAvancarParaEmAndamento()
    {
        var execucao = new Execucao(Guid.NewGuid(), "202611031200");
        execucao.Iniciar();
        execucao.Status.Should().Be(StatusExecucao.EmAndamento);
        execucao.IniciadaEm.Should().NotBeNull();
    }

    [Fact]
    public void Iniciar_QuandoJaIniciada_DeveFalhar()
    {
        var execucao = new Execucao(Guid.NewGuid(), "202611031200");
        execucao.Iniciar();

        var act = () => execucao.Iniciar();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Concluir_QuandoEmAndamento_DeveFinalizarComSucesso()
    {
        var execucao = new Execucao(Guid.NewGuid(), "202611031200");
        execucao.Iniciar();
        execucao.Concluir();

        execucao.Status.Should().Be(StatusExecucao.Concluida);
        execucao.FinalizadaEm.Should().NotBeNull();
    }

    [Fact]
    public void Falhar_QuandoEmAndamento_DeveRegistrarMotivo()
    {
        var execucao = new Execucao(Guid.NewGuid(), "202611031200");
        execucao.Iniciar();
        execucao.Falhar("Peça indisponível");

        execucao.Status.Should().Be(StatusExecucao.Falhou);
        execucao.MotivoFalha.Should().Be("Peça indisponível");
    }

    [Fact]
    public void Falhar_QuandoJaConcluida_DeveFalhar()
    {
        var execucao = new Execucao(Guid.NewGuid(), "202611031200");
        execucao.Iniciar();
        execucao.Concluir();

        var act = () => execucao.Falhar("tarde demais");
        act.Should().Throw<InvalidOperationException>();
    }
}
