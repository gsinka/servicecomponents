using System;
using Autofac;
using Microsoft.Extensions.Caching.Distributed;
using ServiceComponents.Api.Mediator;

namespace ServiceComponents.Infrastructure.Behaviors.CommandConstraints
{
    public static class CommandConstraintAutofacExtensions
    {
        public static ContainerBuilder AddCommandConstraints(this ContainerBuilder builder, Func<ICommand, string> keyBuilder, Func<ICommand, string, (bool, string, TimeSpan?)> constraint)
        {
            builder.Register(context => new CommandExecutionConstraintBehavior(
                context.Resolve<IDistributedCache>(),
                keyBuilder,
                constraint
            )).AsImplementedInterfaces().InstancePerDependency();

            return builder;
        }
    }
}