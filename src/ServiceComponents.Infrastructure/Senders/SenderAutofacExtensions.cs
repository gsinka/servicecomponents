using System;
using System.Net.Http;
using Autofac;
using Microsoft.Extensions.Options;
using Serilog;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Application.Senders;
using ServiceComponents.Infrastructure.Http;
using ServiceComponents.Infrastructure.Http.Senders;

namespace ServiceComponents.Infrastructure.Senders
{
    public static class SenderAutofacExtensions
    {
        public static ContainerBuilder AddCommandRouter(this ContainerBuilder builder, Func<ICommand, object> keySelector)
        {
            builder.Register(context => new CommandRouter(context.Resolve<ILogger>(), context.Resolve<ILifetimeScope>(), keySelector)).As<ISendCommand>().InstancePerLifetimeScope();
            return builder;
        }

        public static ContainerBuilder AddQueryRouter(this ContainerBuilder builder, Func<IQuery, object> keySelector)
        {
            builder.Register(context => new QueryRouter(context.Resolve<ILogger>(), context.Resolve<ILifetimeScope>(), keySelector)).As<ISendQuery>().InstancePerLifetimeScope();
            return builder;
        }
        public static ContainerBuilder AddEventRouter(this ContainerBuilder builder, Func<IEvent, object> keySelector)
        {
            builder.Register(context => new EventRouter(context.Resolve<ILogger>(), context.Resolve<ILifetimeScope>(), keySelector)).As<IPublishEvent>().InstancePerLifetimeScope();
            return builder;
        }

        public static ContainerBuilder AddHttpCommandSender(this ContainerBuilder builder, Uri requestUri, string key = default)
        {
            var proxyRegistration = builder.Register(context => new HttpCommandSenderProxy(key == default ? context.Resolve<ISendHttpCommand>() : context.ResolveKeyed<ISendHttpCommand>(key))).InstancePerDependency();

            var senderRegistration = builder.Register(context => new HttpCommandSender(
                context.Resolve<ILogger>(),
                context.Resolve<IHttpClientFactory>().CreateClient(),
                    requestUri,
                    context.Resolve<IOptions<HttpRequestOptions>>()));

            if (key == default)
            {
                proxyRegistration.As<ISendCommand>();
                senderRegistration.As<ISendHttpCommand>();
            }
            else
            {
                proxyRegistration.Keyed<ISendCommand>(key);
                senderRegistration.Keyed<ISendHttpCommand>(key);
            }

            return builder;
        }

        public static ContainerBuilder AddHttpQuerySender(this ContainerBuilder builder, Uri requestUri, string key = default)
        {
            var proxyRegistration = builder.Register(context => new HttpQuerySenderProxy(key == default ? context.Resolve<ISendHttpQuery>() : context.ResolveKeyed<ISendHttpQuery>(key))).InstancePerDependency();

            var senderRegistration = builder.Register(context => new HttpQuerySender(
                context.Resolve<ILogger>(),
                context.Resolve<IHttpClientFactory>().CreateClient(),
                requestUri,
                context.Resolve<IOptions<HttpRequestOptions>>()));

            if (key == default) {
                proxyRegistration.As<ISendQuery>();
                senderRegistration.As<ISendHttpQuery>();
            }
            else {
                proxyRegistration.Keyed<ISendQuery>(key);
                senderRegistration.Keyed<ISendHttpQuery>(key);
            }

            return builder;
        }

        public static ContainerBuilder AddHttpEventPublisher(this ContainerBuilder builder, Uri requestUri, string key = default)
        {
            var proxyRegistration = builder.Register(context => new HttpEventPublisherProxy(key == default ? context.Resolve<IPublishHttpEvent>() : context.ResolveKeyed<IPublishHttpEvent>(key))).InstancePerDependency();

            var senderRegistration = builder.Register(context => new HttpEventPublisher(
                context.Resolve<ILogger>(),
                context.Resolve<IHttpClientFactory>().CreateClient(),
                requestUri,
                context.Resolve<IOptions<HttpRequestOptions>>()));

            if (key == default) {
                proxyRegistration.As<IPublishEvent>();
                senderRegistration.As<IPublishHttpEvent>();
            }
            else {
                proxyRegistration.Keyed<IPublishEvent>(key);
                senderRegistration.Keyed<IPublishHttpEvent>(key);
            }

            return builder;
        }

        public static ContainerBuilder AddHttpSenderCorrelationBehavior(this ContainerBuilder builder)
        {
            builder.RegisterDecorator<HttpCommandSenderCorrelationBehavior, ISendHttpCommand>();
            builder.RegisterDecorator<HttpQuerySenderCorrelationBehavior, ISendHttpQuery>();
            builder.RegisterDecorator<HttpEventPublisherCorrelationBehavior, IPublishHttpEvent>();

            return builder;
        }

    }
}