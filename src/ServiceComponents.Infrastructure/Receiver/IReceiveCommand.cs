using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using ServiceComponents.Api.Mediator;

namespace ServiceComponents.Infrastructure.Receiver
{
    public interface IReceiveCommand
    {
        Task ReceiveAsync(ICommand command, string correlationId = null, string causationId = null, string userId = null, string userName = null, CancellationToken cancellationToken = default);
    }
}
