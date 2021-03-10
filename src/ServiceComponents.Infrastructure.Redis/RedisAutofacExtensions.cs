using System;
using System.Collections.Generic;
using Autofac;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Infrastructure.Redis.Behaviors;
using StackExchange.Redis;

namespace ServiceComponents.Infrastructure.Redis
{
    public static class RedisAutofacExtensions
    {
        public static ContainerBuilder AddRedis(this ContainerBuilder builder, string connection)
        {
            builder.Register(context => ConnectionMultiplexer.Connect(connection)).As<IConnectionMultiplexer>().SingleInstance();

            return builder;
        }

        public static ContainerBuilder AddRedisCommandConstraints(this ContainerBuilder builder, Func<IList<ICommand>, bool> constraint, int database = -1)
        {
            builder.Register(context => new ParallelExecutionBehavior(context.Resolve<IConnectionMultiplexer>().GetDatabase(database), constraint)).AsImplementedInterfaces().InstancePerDependency();

            return builder;
        }

    }
}
