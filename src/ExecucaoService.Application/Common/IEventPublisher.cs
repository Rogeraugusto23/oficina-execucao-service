using ExecucaoService.Domain.Events;

namespace ExecucaoService.Application.Common;

public interface IEventPublisher
{
    Task PublicarAsync<TEvent>(TEvent evento) where TEvent : IntegrationEvent;
}
