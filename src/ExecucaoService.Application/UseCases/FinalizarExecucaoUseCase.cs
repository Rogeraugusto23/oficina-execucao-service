using ExecucaoService.Application.Common;
using ExecucaoService.Domain.Entities;
using ExecucaoService.Domain.Events;
using ExecucaoService.Domain.Interfaces;

namespace ExecucaoService.Application.UseCases;

/// <summary>
/// Último passo da Saga: conclui (ou marca falha n)a execução mecânica e
/// publica ExecucaoFinalizadaEvent para o OS Service finalizar a OS (sucesso)
/// ou disparar a compensação — cancelar a OS e estornar o pagamento no
/// Billing Service (falha).
/// </summary>
public class FinalizarExecucaoUseCase
{
    private readonly IExecucaoRepository _repository;
    private readonly IEventPublisher _eventPublisher;

    public FinalizarExecucaoUseCase(IExecucaoRepository repository, IEventPublisher eventPublisher)
    {
        _repository = repository;
        _eventPublisher = eventPublisher;
    }

    public async Task<OperationResult<Execucao>> ExecutarAsync(Guid id, bool sucesso, string? motivoFalha = null)
    {
        var execucao = await _repository.ObterPorIdAsync(id);
        if (execucao == null)
            return OperationResult<Execucao>.Falha(TipoErro.NaoEncontrado, "Execução não encontrada.");

        try
        {
            if (sucesso)
                execucao.Concluir();
            else
                execucao.Falhar(motivoFalha ?? "Falha não especificada.");
        }
        catch (InvalidOperationException ex)
        {
            return OperationResult<Execucao>.Falha(TipoErro.OperacaoInvalida, ex.Message);
        }

        await _repository.SalvarAsync(execucao);

        await _eventPublisher.PublicarAsync(new ExecucaoFinalizadaEvent
        {
            CorrelationId = execucao.OrdemServicoId,
            OrdemServicoId = execucao.OrdemServicoId,
            NumeroOS = execucao.NumeroOS,
            Sucesso = sucesso,
            MotivoFalha = sucesso ? null : motivoFalha
        });

        return OperationResult<Execucao>.Ok(execucao);
    }
}
