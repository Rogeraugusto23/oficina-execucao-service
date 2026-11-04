using ExecucaoService.Application.Common;
using ExecucaoService.Domain.Entities;
using ExecucaoService.Domain.Interfaces;

namespace ExecucaoService.Application.UseCases;

public class ConsultarExecucaoUseCase
{
    private readonly IExecucaoRepository _repository;

    public ConsultarExecucaoUseCase(IExecucaoRepository repository)
    {
        _repository = repository;
    }

    public async Task<OperationResult<Execucao>> ExecutarPorOrdemServicoAsync(Guid ordemServicoId)
    {
        var execucao = await _repository.ObterPorOrdemServicoIdAsync(ordemServicoId);
        if (execucao == null)
            return OperationResult<Execucao>.Falha(TipoErro.NaoEncontrado, "Execução não encontrada para esta OS.");

        return OperationResult<Execucao>.Ok(execucao);
    }
}
