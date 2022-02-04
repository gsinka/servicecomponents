using System;
using System.Net.Http;
using Autofac;
using Microsoft.Extensions.Options;
using Serilog;
using ServiceComponents.Application.Senders;
using ServiceComponents.AspNet.Exceptions;

namespace ServiceComponents.AspNet.Http.Senders
{
    public static class HttpSenderAutofacExtensions
    {
        public static ContainerBuilder AddHttpCommandSender(this ContainerBuilder builder, Uri requestUri, object key = default, string httpClientKey = default)
        {
            var proxyRegistration = builder.Register(context => new HttpCommandSenderProxy(key == default ? context.Resolve<ISendHttpCommand>() : context.ResolveKeyed<ISendHttpCommand>(key))).InstancePerDependency();

            var senderRegistration = builder.Register(context => new HttpCommandSender(
                context.Resolve<ILogger>(),
                httpClientKey == default ? context.Resolve<IHttpClientFactory>().CreateClient() : context.Resolve<IHttpClientFactory>().CreateClient(httpClientKey),
                requestUri,
                context.Resolve<IOptions<HttpRequestOptions>>(),
                context.Resolve<IExceptionMapperService>()));

            if (key == default) {
                proxyRegistration.As<ISendCommand>();
                senderRegistration.As<ISendHttpCommand>();
            }
            else {
                proxyRegistration.Keyed<ISendCommand>(key);
                senderRegistration.Keyed<ISendHttpCommand>(key);
            }

            return builder;
        }

        public static ContainerBuilder AddHttpQuerySender(this ContainerBuilder builder, Uri requestUri, object key = default, string httpClientKey = default)
        {
            var proxyRegistration = builder.Register(context => new HttpQuerySenderProxy(key == default ? context.Resolve<ISendHttpQuery>() : context.ResolveKeyed<ISendHttpQuery>(key))).InstancePerDependency();

            var senderRegistration = builder.Register(context => new HttpQuerySender(
                context.Resolve<ILogger>(),
                httpClientKey == default ? context.Resolve<IHttpClientFactory>().CreateClient() : context.Resolve<IHttpClientFactory>().CreateClient(httpClientKey),
                requestUri,
                context.Resolve<IOptions<HttpRequestOptions>>(), 
                context.Resolve<IExceptionMapperService>()));

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

        public static ContainerBuilder AddHttpEventPublisher(this ContainerBuilder builder, Uri requestUri, object key = default, string httpClientKey = default)
        {
            var proxyRegistration = builder.Register(context => new HttpEventPublisherProxy(key == default ? context.Resolve<IPublishHttpEvent>() : context.ResolveKeyed<IPublishHttpEvent>(key))).InstancePerDependency();

            var senderRegistration = builder.Register(context => new HttpEventPublisher(
                context.Resolve<ILogger>(),
                httpClientKey == default ? context.Resolve<IHttpClientFactory>().CreateClient() : context.Resolve<IHttpClientFactory>().CreateClient(httpClientKey),
                requestUri,
                context.Resolve<IOptions<HttpRequestOptions>>(), 
                context.Resolve<IExceptionMapperService>()));

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