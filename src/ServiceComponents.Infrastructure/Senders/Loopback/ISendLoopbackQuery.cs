using System.Threading;
using System.Threading.Tasks;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Application;

namespace ServiceComponents.Infrastructure.Senders
{
    public interface ISendLoopbackQuery
    {
        Task<TResult> SendAsync<TResult>(IQuery<TResult> query, ICorrelation correlation, CancellationToken cancellationToken = default);
    }
}