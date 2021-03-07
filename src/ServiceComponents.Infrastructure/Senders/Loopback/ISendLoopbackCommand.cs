using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ServiceComponents.Api.Mediator;

namespace ServiceComponents.Infrastructure.Senders
{
    public interface ISendLoopbackCommand
    {
        Task SendAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default) where TCommand : ICommand;
    }

    public interface ISendLoopbackQuery
    {
        Task<TResult> SendAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default);
    }

    public interface IPublishLoopbackEvent
    {
        Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : IEvent;
        Task PublishAsync(IEnumerable<IEvent> events, CancellationToken cancellationToken = default);
    }
}