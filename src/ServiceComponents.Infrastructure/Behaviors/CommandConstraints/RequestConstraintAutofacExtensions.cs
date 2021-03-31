using System;
using Autofac;
using Microsoft.Extensions.Caching.Distributed;
using ServiceComponents.Api.Mediator;

namespace ServiceComponents.Infrastructure.Behaviors.CommandConstraints
{
    public static class RequestConstraintAutofacExtensions
    {

        public static ContainerBuilder AddRequestConstraints(this ContainerBuilder builder, Func<IRequest, string[]> keys, Func<string, int, bool> constraint, Func<string, TimeSpan?> expiry)
        {
            builder.Register(context => new RequestExecutionConstraintBehavior(context.Resolve<IDistributedCache>(), keys, constraint, expiry))
                .AsImplementedInterfaces().InstancePerDependency();
            return builder;
        }
    }
}