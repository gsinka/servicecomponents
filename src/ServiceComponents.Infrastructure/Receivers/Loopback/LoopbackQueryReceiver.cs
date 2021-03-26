using System.Threading;
using System.Threading.Tasks;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Application;

namespace ServiceComponents.Infrastructure.Receivers.Loopback
{
    public class LoopbackQueryReceiver : IReceiveLoopbackQuery
    {
        private readonly IReceiveQuery _receiver;

        public LoopbackQueryReceiver(IReceiveQuery receiver)
        {
            _receiver = receiver;
        }

        public async Task<TResult> ReceiveAsync<TResult>(IQuery<TResult> query, ICorrelation correlation, CancellationToken cancellationToken = default)
        {
            return await _receiver.ReceiveAsync(query, cancellationToken);
        }
    }
}