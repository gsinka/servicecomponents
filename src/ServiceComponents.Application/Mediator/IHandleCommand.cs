using System.Threading;
using System.Threading.Tasks;
using ServiceComponents.Api.Mediator;

namespace ServiceComponents.Application.Mediator
{
    public interface IHandleCommand<in T> where T : ICommand
    {
        public Task HandleAsync(T command, CancellationToken cancellationToken = default);
    }
}
