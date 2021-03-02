using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NHibernate;
using NHibernate.Transaction;
using Serilog;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Application;
using ServiceComponents.Application.Mediator;
using ServiceComponents.Application.Senders;
using ServiceComponents.Core;

namespace ServiceComponents.Infrastructure.NHibernate
{
    public abstract class TransactionCommandHandler<T> : CommandHandlerBase<T> where T : ICommand
    {
        private readonly PublisherTransactionSyncWrapper _eventPublisher;
        protected readonly ISession Session;
        public readonly IClock Clock;

        protected TransactionCommandHandler(ILogger log, ICorrelation correlation, ISendQuery querySender, IPublishEvent eventPublisher, ISession session, IClock clock) : base(log, correlation, querySender)
        {
            _eventPublisher = new PublisherTransactionSyncWrapper(log, eventPublisher, session, clock);
            Session = session;
            Clock = clock;
        }

        override public async Task HandleAsync(T command, CancellationToken cancellationToken = default)
        {
            using var tx = Session.BeginTransaction();

            try {

                tx.RegisterSynchronization(_eventPublisher as ITransactionCompletionSynchronization);
                await HandleAsync(command, tx, cancellationToken);
                await tx.CommitAsync(cancellationToken);
            }
            catch {

                await tx.RollbackAsync(cancellationToken);
                throw;
            }
        }

        protected abstract Task HandleAsync(T command, ITransaction transaction, CancellationToken cancellationToken = default);

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
