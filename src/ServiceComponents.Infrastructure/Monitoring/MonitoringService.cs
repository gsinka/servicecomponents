using System;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Application.Mediator;
using ServiceComponents.Application.Monitoring;

namespace ServiceComponents.Infrastructure.Monitoring
{
    public class MonitoringService : IMonitoringService
    {
    }

    public class TracingBehavior : IPreHandleCommand, IPostHandleCommand, IHandleCommandFailure
    {
        public Task PreHandleAsync(ICommand command, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        public Task PostHandleAsync(ICommand command, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task HandleFailureAsync(ICommand command, Exception exception, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }

    public static class TracingAutofacExtensions
    {
        public static ContainerBuilder AddGenericTracing(this ContainerBuilder builder)
        {
            builder.RegisterType<TracingBehavior>().AsImplementedInterfaces().InstancePerDependency();

            return builder;
        }
    }
}
