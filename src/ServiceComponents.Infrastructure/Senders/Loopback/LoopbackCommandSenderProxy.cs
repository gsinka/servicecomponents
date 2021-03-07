using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Application.Senders;

namespace ServiceComponents.Infrastructure.Senders
{
    public class LoopbackCommandSenderProxy : ISendCommand
    {
        private readonly ISendLoopbackCommand _next;

        public LoopbackCommandSenderProxy(ISendLoopbackCommand next)
        {
            _next = next;
        }

        public async Task SendAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default) where TCommand : ICommand
        {
            await _next.SendAsync(command, cancellationToken);
        }
    }

    public class LoopbackQuerySenderProxy : ISendQuery
    {
        private readonly ISendLoopbackQuery _next;

        public LoopbackQuerySenderProxy(ISendLoopbackQuery next)
        {
            _next = next;
        }

        public async Task<TResult> SendAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default)
        {
            return await _next.SendAsync(query, cancellationToken);
        }
    }

    public class LoopbackEventPublisherProxy : IPublishEvent
    {
        private readonly IPublishLoopbackEvent _next;

        public LoopbackEventPublisherProxy(IPublishLoopbackEvent next)
        {
            _next = next;
        }

        public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : IEvent
        {
            await _next.PublishAsync(@event, cancellationToken);
        }

        public async Task PublishAsync(IEnumerable<IEvent> events, CancellationToken cancellationToken = default)
        {
            await _next.PublishAsync(events, cancellationToken);
        }
    }
}