using ExecucaoService.Domain.Entities;

namespace ExecucaoService.Domain.Interfaces;

public interface IExecucaoRepository
{
    Task<Execucao?> ObterPorIdAsync(Guid id);
    Task<Execucao?> ObterPorOrdemServicoIdAsync(Guid ordemServicoId);
    Task SalvarAsync(Execucao execucao);
}
