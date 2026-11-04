using ExecucaoService.Application.Common;
using ExecucaoService.Domain.Entities;
using ExecucaoService.Domain.Interfaces;

namespace ExecucaoService.Application.UseCases;

/// <summary>
/// Disparado ao consumir OrcamentoAprovadoEvent (publicado pelo OS Service
/// quando o cliente aprova o orçamento). Coloca a OS na fila de execução
/// mecânica deste serviço.
/// </summary>
public class EnfileirarExecucaoUseCase
{
    private readonly IExecucaoRepository _repository;

    public EnfileirarExecucaoUseCase(IExecucaoRepository repository)
    {
        _repository = repository;
    }

    public async Task<OperationResult<Execucao>> ExecutarAsync(Guid ordemServicoId, string numeroOS)
    {
        var existente = await _repository.ObterPorOrdemServicoIdAsync(ordemServicoId);
        if (existente != null)
            return OperationResult<Execucao>.Ok(existente, "Execução já estava enfileirada para esta OS.");

        var execucao = new Execucao(ordemServicoId, numeroOS);
        await _repository.SalvarAsync(execucao);

        return OperationResult<Execucao>.Ok(execucao, "OS enfileirada para execução.");
    }
}
