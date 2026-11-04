using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using ExecucaoService.Domain.Entities;
using ExecucaoService.Domain.Enums;
using ExecucaoService.Domain.Interfaces;
using Microsoft.Extensions.Configuration;

namespace ExecucaoService.Infrastructure.Data;

/// <summary>
/// Persistência em DynamoDB (NoSQL) — banco próprio do Execução Service.
/// Mesma limitação conhecida do Billing Service: a consulta por
/// OrdemServicoId usa Scan (ver ADR do Billing Service); evolução futura
/// cria um GSI dedicado.
/// </summary>
public class DynamoDbExecucaoRepository : IExecucaoRepository
{
    private readonly IAmazonDynamoDB _client;
    private readonly string _tableName;

    public DynamoDbExecucaoRepository(IAmazonDynamoDB client, IConfiguration config)
    {
        _client = client;
        _tableName = config["DynamoDb:ExecucoesTableName"] ?? "oficina-wagyu-execucoes";
    }

    public async Task<Execucao?> ObterPorIdAsync(Guid id)
    {
        var table = Table.LoadTable(_client, _tableName);
        var doc = await table.GetItemAsync(id.ToString());
        return doc == null ? null : MapToEntity(doc);
    }

    public async Task<Execucao?> ObterPorOrdemServicoIdAsync(Guid ordemServicoId)
    {
        var table = Table.LoadTable(_client, _tableName);
        var search = table.Scan(new ScanFilter().AddCondition("OrdemServicoId", ScanOperator.Equal, ordemServicoId.ToString()));
        var page = await search.GetNextSetAsync();
        return page.Count > 0 ? MapToEntity(page[0]) : null;
    }

    public async Task SalvarAsync(Execucao execucao)
    {
        var table = Table.LoadTable(_client, _tableName);
        await table.PutItemAsync(MapToDocument(execucao));
    }

    private static Document MapToDocument(Execucao e)
    {
        var doc = new Document
        {
            ["Id"] = e.Id.ToString(),
            ["OrdemServicoId"] = e.OrdemServicoId.ToString(),
            ["NumeroOS"] = e.NumeroOS,
            ["Status"] = e.Status.ToString(),
            ["EntrouNaFilaEm"] = e.EntrouNaFilaEm.ToString("O")
        };
        if (e.IniciadaEm != null) doc["IniciadaEm"] = e.IniciadaEm.Value.ToString("O");
        if (e.FinalizadaEm != null) doc["FinalizadaEm"] = e.FinalizadaEm.Value.ToString("O");
        if (e.MotivoFalha != null) doc["MotivoFalha"] = e.MotivoFalha;
        return doc;
    }

    private static Execucao MapToEntity(Document doc)
    {
        return Execucao.Reidratar(
            id: Guid.Parse(doc["Id"].AsString()),
            ordemServicoId: Guid.Parse(doc["OrdemServicoId"].AsString()),
            numeroOS: doc["NumeroOS"].AsString(),
            status: Enum.Parse<StatusExecucao>(doc["Status"].AsString()),
            entrouNaFilaEm: DateTime.Parse(doc["EntrouNaFilaEm"].AsString(), null, System.Globalization.DateTimeStyles.RoundtripKind),
            iniciadaEm: doc.TryGetValue("IniciadaEm", out var iniciadaEm)
                ? DateTime.Parse(iniciadaEm.AsString(), null, System.Globalization.DateTimeStyles.RoundtripKind)
                : null,
            finalizadaEm: doc.TryGetValue("FinalizadaEm", out var finalizadaEm)
                ? DateTime.Parse(finalizadaEm.AsString(), null, System.Globalization.DateTimeStyles.RoundtripKind)
                : null,
            motivoFalha: doc.TryGetValue("MotivoFalha", out var motivoFalha) ? motivoFalha.AsString() : null);
    }
}
