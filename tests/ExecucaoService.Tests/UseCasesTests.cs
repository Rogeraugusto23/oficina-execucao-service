using ExecucaoService.Application.UseCases;
using ExecucaoService.Domain.Entities;
using ExecucaoService.Domain.Events;
using ExecucaoService.Tests.Fakes;
using FluentAssertions;

namespace ExecucaoService.Tests;

public class UseCasesTests
{
    [Fact]
    public async Task EnfileirarExecucao_DeveCriarRegistroNaFila()
    {
        var repository = new FakeExecucaoRepository();
        var useCase = new EnfileirarExecucaoUseCase(repository);
        var ordemServicoId = Guid.NewGuid();

        var resultado = await useCase.ExecutarAsync(ordemServicoId, "202611031200");

        resultado.Sucesso.Should().BeTrue();
        resultado.Dados!.OrdemServicoId.Should().Be(ordemServicoId);
    }

    [Fact]
    public async Task EnfileirarExecucao_Chamado2Vezes_NaoDeveDuplicar()
    {
        var repository = new FakeExecucaoRepository();
        var useCase = new EnfileirarExecucaoUseCase(repository);
        var ordemServicoId = Guid.NewGuid();

        var primeiro = await useCase.ExecutarAsync(ordemServicoId, "202611031200");
        var segundo = await useCase.ExecutarAsync(ordemServicoId, "202611031200");

        primeiro.Dados!.Id.Should().Be(segundo.Dados!.Id);
    }

    [Fact]
    public async Task FinalizarExecucao_ComSucesso_DevePublicarExecucaoFinalizadaEvent()
    {
        var repository = new FakeExecucaoRepository();
        var publisher = new FakeEventPublisher();

        var execucao = new Execucao(Guid.NewGuid(), "202611031200");
        execucao.Iniciar();
        await repository.SalvarAsync(execucao);

        var useCase = new FinalizarExecucaoUseCase(repository, publisher);
        var resultado = await useCase.ExecutarAsync(execucao.Id, sucesso: true);

        resultado.Sucesso.Should().BeTrue();
        var evento = publisher.EventosPublicados.Should().ContainSingle().Subject.Should().BeOfType<ExecucaoFinalizadaEvent>().Subject;
        evento.Sucesso.Should().BeTrue();
    }

    [Fact]
    public async Task FinalizarExecucao_ComFalha_DevePublicarEventoComMotivo()
    {
        var repository = new FakeExecucaoRepository();
        var publisher = new FakeEventPublisher();

        var execucao = new Execucao(Guid.NewGuid(), "202611031200");
        execucao.Iniciar();
        await repository.SalvarAsync(execucao);

        var useCase = new FinalizarExecucaoUseCase(repository, publisher);
        var resultado = await useCase.ExecutarAsync(execucao.Id, sucesso: false, motivoFalha: "Peça indisponível");

        resultado.Sucesso.Should().BeTrue();
        var evento = publisher.EventosPublicados.Should().ContainSingle().Subject.Should().BeOfType<ExecucaoFinalizadaEvent>().Subject;
        evento.Sucesso.Should().BeFalse();
        evento.MotivoFalha.Should().Be("Peça indisponível");
    }

    [Fact]
    public async Task IniciarExecucao_QuandoNaoEncontrada_DeveFalhar()
    {
        var repository = new FakeExecucaoRepository();
        var useCase = new IniciarExecucaoUseCase(repository);

        var resultado = await useCase.ExecutarAsync(Guid.NewGuid());

        resultado.Sucesso.Should().BeFalse();
    }
}
