using System;
using System.Net.Http;
using Autofac;
using Serilog;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Application.Senders;

namespace ServiceComponents.Infrastructure.Senders
{
    public static class SenderAutofacExtensions
    {
        public static ContainerBuilder AddCommandRouter(this ContainerBuilder builder,
            Func<ICommand, object> keySelector)
        {
            builder.Register(context =>
                    new CommandRouter(context.Resolve<ILogger>(), context.Resolve<ILifetimeScope>(), keySelector))
                .As<ISendCommand>().InstancePerLifetimeScope();
            return builder;
        }

        public static ContainerBuilder AddQueryRouter(this ContainerBuilder builder, Func<IQuery, object> keySelector)
        {
            builder.Register(context =>
                    new QueryRouter(context.Resolve<ILogger>(), context.Resolve<ILifetimeScope>(), keySelector))
                .As<ISendQuery>().InstancePerLifetimeScope();
            return builder;
        }

        public static ContainerBuilder AddEventRouter(this ContainerBuilder builder, Func<IEvent, object> keySelector)
        {
            builder.Register(context =>
                    new EventRouter(context.Resolve<ILogger>(), context.Resolve<ILifetimeScope>(), keySelector))
                .As<IPublishEvent>().InstancePerLifetimeScope();
            return builder;
        }
    }
}