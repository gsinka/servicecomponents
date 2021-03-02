using System.Threading;
using System.Threading.Tasks;
using ServiceComponents.Api.Mediator;

namespace ServiceComponents.AspNet.Http
{
    public interface IReceiveHttpCommand
    {
        Task ReceiveAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default) where TCommand : ICommand;
    }
}
