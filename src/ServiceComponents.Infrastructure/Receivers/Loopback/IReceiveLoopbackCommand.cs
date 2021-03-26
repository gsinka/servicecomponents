using System.Threading;
using System.Threading.Tasks;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Application;

namespace ServiceComponents.Infrastructure.Receivers.Loopback
{
    public interface IReceiveLoopbackCommand
    {
        Task ReceiveAsync<TCommand>(TCommand command, ICorrelation correlation, CancellationToken cancellationToken = default) where TCommand : ICommand;
    }
}