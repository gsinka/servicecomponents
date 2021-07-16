using System;
using System.Threading;
using System.Threading.Tasks;
using ServiceComponents.Api.Mediator;

namespace ServiceComponents.Application
{
    public interface IEventRecorder
    {
        Task<T> WaitFor<T>(Func<T, ICorrelation, bool> filter, TimeSpan timeOut) where T : IEvent;

        Task<T> WaitFor<T>(Func<T, ICorrelation, bool> filter, CancellationToken cancellationToken = default) where T : IEvent;
    }
}
