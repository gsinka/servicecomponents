using System;
using System.Threading;
using System.Threading.Tasks;
using ServiceComponents.Api.Mediator;

namespace ServiceComponents.Application.Mediator
{
    public interface IHandleCommandFailure
    {
        Task HandleFailureAsync(ICommand command, Exception exception, CancellationToken cancellationToken = default);
    }

    public interface IHandleCommandFailure<in T> where T : ICommand
    {
        Task HandleFailureAsync(T command, Exception exception, CancellationToken cancellationToken = default);
    }
}