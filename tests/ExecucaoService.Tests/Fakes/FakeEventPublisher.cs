using ExecucaoService.Application.Common;
using ExecucaoService.Domain.Events;

namespace ExecucaoService.Tests.Fakes;

public class FakeEventPublisher : IEventPublisher
{
    public List<IntegrationEvent> EventosPublicados { get; } = new();

    public Task PublicarAsync<TEvent>(TEvent evento) where TEvent : IntegrationEvent
    {
        EventosPublicados.Add(evento);
        return Task.CompletedTask;
    }
}
