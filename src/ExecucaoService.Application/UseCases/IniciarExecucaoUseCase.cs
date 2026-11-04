using ExecucaoService.Application.Common;
using ExecucaoService.Domain.Entities;
using ExecucaoService.Domain.Interfaces;

namespace ExecucaoService.Application.UseCases;

public class IniciarExecucaoUseCase
{
    private readonly IExecucaoRepository _repository;

    public IniciarExecucaoUseCase(IExecucaoRepository repository)
    {
        _repository = repository;
    }

    public async Task<OperationResult<Execucao>> ExecutarAsync(Guid id)
    {
        var execucao = await _repository.ObterPorIdAsync(id);
        if (execucao == null)
            return OperationResult<Execucao>.Falha(TipoErro.NaoEncontrado, "Execução não encontrada.");

        try
        {
            execucao.Iniciar();
        }
        catch (InvalidOperationException ex)
        {
            return OperationResult<Execucao>.Falha(TipoErro.OperacaoInvalida, ex.Message);
        }

        await _repository.SalvarAsync(execucao);
        return OperationResult<Execucao>.Ok(execucao);
    }
}
