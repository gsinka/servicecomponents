using System.Threading;
using System.Threading.Tasks;
using ServiceComponents.Api.Mediator;

namespace ServiceComponents.Infrastructure.Dispatchers
{
    public interface IDispatchCommand
    {
        Task DispatchAsync<T>(T command, CancellationToken cancellationToken = default) where T : ICommand;
    }
}
