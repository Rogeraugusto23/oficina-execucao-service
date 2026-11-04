using ExecucaoService.Domain.Enums;

namespace ExecucaoService.Domain.Entities;

/// <summary>
/// Agregado raiz do Execução Service — representa a fila/execução mecânica
/// de uma OrdemServico já com orçamento aprovado. Banco próprio (DynamoDB);
/// nenhum outro serviço acessa esta tabela diretamente.
/// </summary>
public class Execucao
{
    public Guid Id { get; private set; }
    public Guid OrdemServicoId { get; private set; }
    public string NumeroOS { get; private set; }
    public StatusExecucao Status { get; private set; }
    public DateTime EntrouNaFilaEm { get; private set; }
    public DateTime? IniciadaEm { get; private set; }
    public DateTime? FinalizadaEm { get; private set; }
    public string? MotivoFalha { get; private set; }

    private Execucao() { NumeroOS = ""; }

    public Execucao(Guid ordemServicoId, string numeroOS)
    {
        Id = Guid.NewGuid();
        OrdemServicoId = ordemServicoId;
        NumeroOS = numeroOS;
        Status = StatusExecucao.NaFila;
        EntrouNaFilaEm = DateTime.UtcNow;
    }

    public static Execucao Reidratar(
        Guid id, Guid ordemServicoId, string numeroOS, StatusExecucao status,
        DateTime entrouNaFilaEm, DateTime? iniciadaEm, DateTime? finalizadaEm, string? motivoFalha)
    {
        return new Execucao
        {
            Id = id,
            OrdemServicoId = ordemServicoId,
            NumeroOS = numeroOS,
            Status = status,
            EntrouNaFilaEm = entrouNaFilaEm,
            IniciadaEm = iniciadaEm,
            FinalizadaEm = finalizadaEm,
            MotivoFalha = motivoFalha
        };
    }

    public void Iniciar()
    {
        if (Status != StatusExecucao.NaFila)
            throw new InvalidOperationException($"Execução precisa estar Na Fila para ser iniciada (status atual: {Status}).");

        Status = StatusExecucao.EmAndamento;
        IniciadaEm = DateTime.UtcNow;
    }

    public void Concluir()
    {
        if (Status != StatusExecucao.EmAndamento)
            throw new InvalidOperationException($"Execução precisa estar Em Andamento para ser concluída (status atual: {Status}).");

        Status = StatusExecucao.Concluida;
        FinalizadaEm = DateTime.UtcNow;
    }

    public void Falhar(string motivo)
    {
        if (Status is StatusExecucao.Concluida or StatusExecucao.Falhou)
            throw new InvalidOperationException($"Execução já foi finalizada (status atual: {Status}).");

        Status = StatusExecucao.Falhou;
        MotivoFalha = motivo;
        FinalizadaEm = DateTime.UtcNow;
    }
}
