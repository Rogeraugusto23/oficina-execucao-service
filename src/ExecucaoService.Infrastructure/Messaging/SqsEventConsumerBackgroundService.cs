using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using ExecucaoService.Application.UseCases;
using ExecucaoService.Domain.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ExecucaoService.Infrastructure.Messaging;

/// <summary>
/// Consome a fila SQS do Execução Service (subscrita ao tópico SNS de
/// eventos de integração). Trata apenas OrcamentoAprovadoEvent, enfileirando
/// a OS para execução — ver EnfileirarExecucaoUseCase.
/// </summary>
public class SqsEventConsumerBackgroundService : BackgroundService
{
    private readonly IAmazonSQS _sqs;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SqsEventConsumerBackgroundService> _logger;
    private readonly string? _queueUrl;

    public SqsEventConsumerBackgroundService(
        IAmazonSQS sqs,
        IServiceScopeFactory scopeFactory,
        IConfiguration config,
        ILogger<SqsEventConsumerBackgroundService> logger)
    {
        _sqs = sqs;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _queueUrl = config["Messaging:SqsQueueUrl"];
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(_queueUrl))
        {
            _logger.LogWarning("Messaging:SqsQueueUrl não configurado — consumidor de eventos desligado.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var response = await _sqs.ReceiveMessageAsync(new ReceiveMessageRequest
                {
                    QueueUrl = _queueUrl,
                    MaxNumberOfMessages = 10,
                    WaitTimeSeconds = 10,
                    MessageAttributeNames = new List<string> { "EventType" }
                }, stoppingToken);

                foreach (var message in response.Messages)
                {
                    await ProcessarMensagemAsync(message);
                    await _sqs.DeleteMessageAsync(_queueUrl, message.ReceiptHandle, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Encerramento normal do serviço.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao consumir fila SQS de eventos de integração");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task ProcessarMensagemAsync(Message message)
    {
        if (!message.MessageAttributes.TryGetValue("EventType", out var eventTypeAttr))
        {
            _logger.LogWarning("Mensagem SQS sem atributo EventType, ignorada: {MessageId}", message.MessageId);
            return;
        }

        using var scope = _scopeFactory.CreateScope();

        switch (eventTypeAttr.StringValue)
        {
            case nameof(OrcamentoAprovadoEvent):
            {
                var evento = JsonSerializer.Deserialize<OrcamentoAprovadoEvent>(message.Body)!;
                var useCase = scope.ServiceProvider.GetRequiredService<EnfileirarExecucaoUseCase>();
                await useCase.ExecutarAsync(evento.OrdemServicoId, evento.NumeroOS);
                break;
            }
            default:
                _logger.LogDebug("Evento {EventType} ignorado pelo Execução Service", eventTypeAttr.StringValue);
                break;
        }
    }
}
