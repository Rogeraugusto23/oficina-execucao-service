using ExecucaoService.Application.Common;
using ExecucaoService.Application.UseCases;
using ExecucaoService.Domain.Entities;
using ExecucaoService.Domain.Enums;
using ExecucaoService.Domain.Events;
using ExecucaoService.Tests.Fakes;
using FluentAssertions;

namespace ExecucaoService.Tests;

public class UseCasesComplementaresTests
{
    private static async Task<(FakeExecucaoRepository repo, Execucao execucao)> CriarNaFilaAsync()
    {
        var repo = new FakeExecucaoRepository();
        var resultado = await new EnfileirarExecucaoUseCase(repo).ExecutarAsync(Guid.NewGuid(), "202611050001");
        return (repo, resultado.Dados!);
    }

    [Fact]
    public async Task Consultar_QuandoExiste_DeveRetornarExecucao()
    {
        var (repo, execucao) = await CriarNaFilaAsync();

        var resultado = await new ConsultarExecucaoUseCase(repo).ExecutarPorOrdemServicoAsync(execucao.OrdemServicoId);

        resultado.Sucesso.Should().BeTrue();
        resultado.Dados!.Id.Should().Be(execucao.Id);
    }

    [Fact]
    public async Task Consultar_QuandoNaoExiste_DeveRetornarNaoEncontrado()
    {
        var resultado = await new ConsultarExecucaoUseCase(new FakeExecucaoRepository())
            .ExecutarPorOrdemServicoAsync(Guid.NewGuid());

        resultado.Sucesso.Should().BeFalse();
        resultado.Erro.Should().Be(TipoErro.NaoEncontrado);
    }

    [Fact]
    public async Task Iniciar_QuandoNaFila_DeveIniciarESalvar()
    {
        var (repo, execucao) = await CriarNaFilaAsync();

        var resultado = await new IniciarExecucaoUseCase(repo).ExecutarAsync(execucao.Id);

        resultado.Sucesso.Should().BeTrue();
        resultado.Dados!.Status.Should().Be(StatusExecucao.EmAndamento);
        resultado.Dados.IniciadaEm.Should().NotBeNull();
    }

    [Fact]
    public async Task Iniciar_QuandoJaIniciada_DeveRetornarOperacaoInvalida()
    {
        var (repo, execucao) = await CriarNaFilaAsync();
        var useCase = new IniciarExecucaoUseCase(repo);
        await useCase.ExecutarAsync(execucao.Id);

        var resultado = await useCase.ExecutarAsync(execucao.Id);

        resultado.Sucesso.Should().BeFalse();
        resultado.Erro.Should().Be(TipoErro.OperacaoInvalida);
    }

    [Fact]
    public async Task Finalizar_QuandoNaoEncontrada_DeveRetornarNaoEncontradoESemEvento()
    {
        var publisher = new FakeEventPublisher();

        var resultado = await new FinalizarExecucaoUseCase(new FakeExecucaoRepository(), publisher)
            .ExecutarAsync(Guid.NewGuid(), true);

        resultado.Sucesso.Should().BeFalse();
        resultado.Erro.Should().Be(TipoErro.NaoEncontrado);
        publisher.EventosPublicados.Should().BeEmpty();
    }

    [Fact]
    public async Task Finalizar_QuandoAindaNaFila_DeveRetornarOperacaoInvalidaESemEvento()
    {
        var (repo, execucao) = await CriarNaFilaAsync();
        var publisher = new FakeEventPublisher();

        var resultado = await new FinalizarExecucaoUseCase(repo, publisher).ExecutarAsync(execucao.Id, true);

        resultado.Sucesso.Should().BeFalse();
        resultado.Erro.Should().Be(TipoErro.OperacaoInvalida);
        publisher.EventosPublicados.Should().BeEmpty();
    }

    [Fact]
    public async Task Finalizar_ComFalhaSemMotivo_DeveUsarMensagemPadrao()
    {
        var (repo, execucao) = await CriarNaFilaAsync();
        await new IniciarExecucaoUseCase(repo).ExecutarAsync(execucao.Id);

        var resultado = await new FinalizarExecucaoUseCase(repo, new FakeEventPublisher())
            .ExecutarAsync(execucao.Id, false);

        resultado.Sucesso.Should().BeTrue();
        resultado.Dados!.Status.Should().Be(StatusExecucao.Falhou);
        resultado.Dados.MotivoFalha.Should().Be("Falha não especificada.");
    }

    [Fact]
    public void Reidratar_DevePreservarTodosOsCampos()
    {
        var id = Guid.NewGuid();
        var osId = Guid.NewGuid();
        var entrou = DateTime.UtcNow.AddHours(-2);
        var iniciou = DateTime.UtcNow.AddHours(-1);

        var e = Execucao.Reidratar(id, osId, "123", StatusExecucao.EmAndamento, entrou, iniciou, null, null);

        e.Id.Should().Be(id);
        e.OrdemServicoId.Should().Be(osId);
        e.NumeroOS.Should().Be("123");
        e.Status.Should().Be(StatusExecucao.EmAndamento);
        e.EntrouNaFilaEm.Should().Be(entrou);
        e.IniciadaEm.Should().Be(iniciou);
    }

    [Fact]
    public void Eventos_DevemTerCorrelationIdEOcorridoEmPadrao()
    {
        var evento = new ExecucaoFinalizadaEvent { OrdemServicoId = Guid.NewGuid(), NumeroOS = "1", Sucesso = true };
        var aprovado = new OrcamentoAprovadoEvent { OrdemServicoId = Guid.NewGuid(), NumeroOS = "1", ValorTotal = 10m };

        evento.OcorridoEm.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        aprovado.ValorTotal.Should().Be(10m);
        evento.CorrelationId.Should().Be(Guid.Empty);
    }
}
