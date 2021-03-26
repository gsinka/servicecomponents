using System.Threading;
using System.Threading.Tasks;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Application;

namespace ServiceComponents.Infrastructure.Receivers.Loopback
{
    public interface IReceiveLoopbackQuery
    {
        Task<TResult> ReceiveAsync<TResult>(IQuery<TResult> query, ICorrelation correlation, CancellationToken cancellationToken = default);
    }
}