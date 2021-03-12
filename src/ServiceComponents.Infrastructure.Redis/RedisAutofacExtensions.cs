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
        public static ContainerBuilder AddRedisConnection(this ContainerBuilder builder, string connection, object key = default)
        {
            var reg = builder.Register(context => ConnectionMultiplexer.Connect(connection)).SingleInstance();

            if (key == default) {
                reg.As<IConnectionMultiplexer>();
            }
            else {
                reg.Keyed<IConnectionMultiplexer>(key);
            }

            return builder;
        }

        public static ContainerBuilder AddRedisDatabase(this ContainerBuilder builder, int database = 0, object connectionKey = default, object key = default)
        {
            var reg = builder.Register(context =>
                (connectionKey == default
                    ? context.Resolve<IConnectionMultiplexer>()
                    : context.ResolveKeyed<IConnectionMultiplexer>(connectionKey))
                .GetDatabase(database));

            if (key == default) {
                reg.As<IDatabase>();
            }
            else {
                reg.Keyed<IDatabase>(key);
            }

            return builder;
        }

        public static ContainerBuilder AddRedisCommandConstraints(this ContainerBuilder builder, Func<ICommand, IList<ICommand>, bool> constraint, Func<ICommand, TimeSpan?> expiryFunc, object databaseKey = default)
        {
            builder.Register(context => new ParallelExecutionBehavior(
                databaseKey == default ? context.Resolve<IDatabase>() : context.ResolveKeyed<IDatabase>(databaseKey),
                constraint,
                expiryFunc)).AsImplementedInterfaces().InstancePerDependency();

            return builder;
        }
    }
}
