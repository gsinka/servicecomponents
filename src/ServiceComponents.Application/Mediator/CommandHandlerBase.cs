using System.Threading;
using System.Threading.Tasks;
using Serilog;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Application.Senders;

namespace ServiceComponents.Application.Mediator
{
    public abstract class CommandHandlerBase<TCommand> : IHandleCommand<TCommand> where TCommand : ICommand
    {
        protected readonly ICorrelation Correlation;
        private readonly ISendQuery _querySender;
        
        protected ILogger Log { get; }

        protected CommandHandlerBase(ILogger log, ICorrelation correlation, ISendQuery querySender)
        {
            Correlation = correlation;
            _querySender = querySender;
            Log = log;
        }

        public abstract Task HandleAsync(TCommand command, CancellationToken cancellationToken = default);

        protected async Task<TResult> SendAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default)
        {
            return await _querySender.SendAsync(query, cancellationToken);
        }
    }
}