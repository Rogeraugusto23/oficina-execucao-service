namespace ExecucaoService.Domain.Events;

/// <summary>
/// Contrato de mensageria compartilhado com o OS Service e o Billing
/// Service (mesmo shape JSON; nenhuma referência de código entre os
/// repositórios).
/// </summary>
public abstract record IntegrationEvent
{
    public Guid CorrelationId { get; init; }
    public DateTime OcorridoEm { get; init; } = DateTime.UtcNow;
}

// ---------- Consumido (publicado pelo OS Service) ----------

public sealed record OrcamentoAprovadoEvent : IntegrationEvent
{
    public Guid OrdemServicoId { get; init; }
    public string NumeroOS { get; init; } = "";
    public decimal ValorTotal { get; init; }
}

// ---------- Publicado pelo Execução Service ----------

/// <summary>
/// Passo final da Saga: o OS Service consome este evento para avançar a OS
/// para Finalizada (Sucesso=true) ou disparar a compensação — cancelar a OS
/// e sinalizar o Billing Service para estornar o pagamento (Sucesso=false).
/// </summary>
public sealed record ExecucaoFinalizadaEvent : IntegrationEvent
{
    public Guid OrdemServicoId { get; init; }
    public string NumeroOS { get; init; } = "";
    public bool Sucesso { get; init; }
    public string? MotivoFalha { get; init; }
}
