using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Application.Senders;

namespace ServiceComponents.Application.Mediator
{
    [Obsolete("Please use HandleCommand instead of this")]
    public abstract class CommandHandler<TCommand> : IHandleCommand<TCommand> where TCommand : ICommand
    {
        protected readonly ICorrelation Correlation;
        private readonly ISendQuery _querySender;
        private readonly IPublishEvent _eventPublisher;

        protected ILogger Log { get; }

        protected CommandHandler(ILogger log, ICorrelation correlation, ISendQuery querySender, IPublishEvent eventPublisher)
        {
            Correlation = correlation;
            Log = log;
            _querySender = querySender;
            _eventPublisher = eventPublisher;
        }

        public abstract Task HandleAsync(TCommand command, CancellationToken cancellationToken = default);

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

    [Obsolete("Please use HandleQuery instead of this")]
    public abstract class QueryHandler<TQuery, TResult> : IHandleQuery<TQuery, TResult> where TQuery : IQuery<TResult>
    {
        private readonly ISendQuery _querySender;
        protected ILogger Log { get; }
        protected ICorrelation Correlation { get; }

        protected QueryHandler(ILogger log, ICorrelation correlation, ISendQuery querySender)
        {
            _querySender = querySender;
            Log = log;
            Correlation = correlation;
        }

        public abstract Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken = default);

        protected async Task<T> SendAsync<T>(IQuery<T> query, CancellationToken cancellationToken = default)
        {
            return await _querySender.SendAsync(query, cancellationToken);
        }
    }

    [Obsolete("Deprecated because of namespace conflict. Please use HandleEvent.")]
    public abstract class EventHandler<TEvent> : IHandleEvent<TEvent> where TEvent : IEvent
    {
        protected readonly ILogger Log;
        protected readonly ICorrelation Correlation;
        private readonly ISendCommand _commandSender;
        private readonly ISendQuery _querySender;

        protected EventHandler(ILogger log, ICorrelation correlation, ISendCommand commandSender, ISendQuery querySender)
        {
            Log = log;
            Correlation = correlation;
            _commandSender = commandSender;
            _querySender = querySender;
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
    }
}