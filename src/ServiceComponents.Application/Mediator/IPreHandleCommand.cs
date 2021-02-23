using System.Threading;
using System.Threading.Tasks;
using ServiceComponents.Api.Mediator;

namespace ServiceComponents.Application.Mediator
{
    public interface IPreHandleCommand
    {
        Task PreHandleAsync(ICommand command, CancellationToken cancellationToken = default);
    }

    public interface IPreHandleCommand<in T> where T : ICommand
    {
        Task PreHandleAsync(T command, CancellationToken cancellationToken = default);
    }
}