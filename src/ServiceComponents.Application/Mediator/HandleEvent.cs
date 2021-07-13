using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Application.Senders;

namespace ServiceComponents.Application.Mediator
{
    public abstract class HandleEvent<TEvent> : IHandleEvent<TEvent> where TEvent : IEvent
    {
        protected readonly ILogger Log;
        protected readonly ICorrelation Correlation;
        private readonly ISendCommand _commandSender;
        private readonly ISendQuery _querySender;
        private readonly IPublishEvent _eventPublisher;

        protected HandleEvent(ILogger log, ICorrelation correlation, ISendCommand commandSender, ISendQuery querySender, IPublishEvent eventPublisher)
        {
            Log = log;
            Correlation = correlation;

            _commandSender = commandSender;
            _querySender = querySender;
            _eventPublisher = eventPublisher;
        }

        public abstract Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default);

        protected async Task SendAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default) where TCommand : ICommand
        {
            await _commandSender.SendAsync(command, cancellationToken);
        }

        protected async Task<TResult> SendAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default)
        {
            return await _querySender.SendAsync(query, cancellationToken);
        }

        protected async Task PublishAsync<TEvent>(TEvent @event, IDictionary<string, string> args = default, CancellationToken cancellationToken = default) where TEvent : IEvent
        {
            await _eventPublisher.PublishAsync(@event, args, cancellationToken);
        }

        protected async Task PublishAsync(IEnumerable<IEvent> events, IDictionary<string, string> args = default, CancellationToken cancellationToken = default)
        {
            await _eventPublisher.PublishAsync(events, args, cancellationToken);
        }
    }
}