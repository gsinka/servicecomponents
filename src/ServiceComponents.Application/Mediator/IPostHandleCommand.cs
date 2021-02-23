using System.Threading;
using System.Threading.Tasks;
using ServiceComponents.Api.Mediator;

namespace ServiceComponents.Application.Mediator
{
    public interface IPostHandleCommand
    {
        Task PostHandleAsync(ICommand command, CancellationToken cancellationToken = default);
    }

    public interface IPostHandleCommand<in T> where T : ICommand
    {
        Task PostHandleAsync(T command, CancellationToken cancellationToken = default);
    }
}