using System.Threading;
using System.Threading.Tasks;
using NHibernate;
using NHibernate.Transaction;
using Serilog;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Application;
using ServiceComponents.Application.Mediator;
using ServiceComponents.Application.Senders;

namespace ServiceComponents.Infrastructure.NHibernate
{
    public abstract class TransactionCommandHandler<T> : CommandHandlerBase<T> where T : ICommand
    {
        private readonly NHibernateEventPublisher _eventPublisher;
        protected readonly ISession Session;

        protected TransactionCommandHandler(ILogger log, ICorrelation correlation, ISendQuery querySender, NHibernateEventPublisher eventPublisher, ISession session) : base(log, correlation, querySender)
        {
            _eventPublisher = eventPublisher;
            Session = session;
        }

        override public async Task HandleAsync(T command, CancellationToken cancellationToken = default)
        {
            using var tx = Session.BeginTransaction();

            try {
                
                tx.RegisterSynchronization(new AfterTransactionCompletes(txResult => {  }));
                await HandleAsync(command, tx, cancellationToken);

            }
            catch {

                await tx.RollbackAsync(cancellationToken);
                throw;
            }
        }

        protected abstract Task HandleAsync(T command, ITransaction transaction, CancellationToken cancellationToken = default);
    }
}
