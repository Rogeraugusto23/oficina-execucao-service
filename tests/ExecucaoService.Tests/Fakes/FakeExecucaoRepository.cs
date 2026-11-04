using ExecucaoService.Domain.Entities;
using ExecucaoService.Domain.Interfaces;

namespace ExecucaoService.Tests.Fakes;

public class FakeExecucaoRepository : IExecucaoRepository
{
    private readonly Dictionary<Guid, Execucao> _porId = new();

    public Task<Execucao?> ObterPorIdAsync(Guid id) =>
        Task.FromResult(_porId.GetValueOrDefault(id));

    public Task<Execucao?> ObterPorOrdemServicoIdAsync(Guid ordemServicoId) =>
        Task.FromResult(_porId.Values.FirstOrDefault(e => e.OrdemServicoId == ordemServicoId));

    public Task SalvarAsync(Execucao execucao)
    {
        _porId[execucao.Id] = execucao;
        return Task.CompletedTask;
    }
}
