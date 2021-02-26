using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Application.Senders;

namespace ServiceComponents.Application.Mediator
{
    public abstract class CommandHandler<TCommand> : CommandHandlerBase<TCommand> where TCommand : ICommand
    {
        private readonly IPublishEvent _eventPublisher;

        protected CommandHandler(ILogger log, ICorrelation correlation, ISendQuery querySender, IPublishEvent eventPublisher) : base(log, correlation, querySender)
        {
            _eventPublisher = eventPublisher;
        }

        protected async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : IEvent
        {
            await _eventPublisher.PublishAsync(@event, cancellationToken);
        }

        protected async Task PublishAsync(IEnumerable<IEvent> events, CancellationToken cancellationToken = default)
        {
            await _eventPublisher.PublishAsync(events, cancellationToken);
        }
    }
}