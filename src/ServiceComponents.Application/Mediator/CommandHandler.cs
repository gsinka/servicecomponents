using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Application.Senders;

namespace ServiceComponents.Application.Mediator
{
    public abstract class CommandHandler<TCommand> : IHandleCommand<TCommand> where TCommand : ICommand
    {
        protected readonly ICorrelation Correlation;
        private readonly ISendQuery _querySender;
        private readonly IPublishEvent _eventPublisher;

        protected ILogger Log { get; }

        protected CommandHandler(ILogger log, ICorrelation correlation, ISendQuery querySender, IPublishEvent eventPublisher)
        {
            Correlation = correlation;
            _querySender = querySender;
            _eventPublisher = eventPublisher;
            Log = log;
        }

        public abstract Task HandleAsync(TCommand command, CancellationToken cancellationToken = default);

        protected async Task<TResult> SendAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default)
        {
            return await _querySender.SendAsync(query, cancellationToken);
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