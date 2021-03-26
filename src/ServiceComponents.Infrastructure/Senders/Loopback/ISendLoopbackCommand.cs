using System.Threading;
using System.Threading.Tasks;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Application;

namespace ServiceComponents.Infrastructure.Senders.Loopback
{
    public interface ISendLoopbackCommand
    {
        Task SendAsync<TCommand>(TCommand command, ICorrelation correlation, CancellationToken cancellationToken = default) where TCommand : ICommand;
    }
}