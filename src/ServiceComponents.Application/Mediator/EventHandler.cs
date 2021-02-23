using System.Threading;
using System.Threading.Tasks;
using Serilog;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Application.Senders;

namespace ServiceComponents.Application.Mediator
{
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