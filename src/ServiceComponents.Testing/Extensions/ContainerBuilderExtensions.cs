using System;
using Autofac;
using Microsoft.Extensions.Options;
using Serilog;
using ServiceComponents.Application.Senders;
using ServiceComponents.AspNet.Http.Senders;
using HttpRequestOptions = ServiceComponents.AspNet.Http.HttpRequestOptions;

namespace ServiceComponents.Testing.Extensions
{
    public static class ContainerBuilderExtensions
    {
        public static void UseTestPublisher(this ContainerBuilder builder, IPublishEvent publisher)
        {
            builder.RegisterInstance(publisher);
        }

        public static void UseTestSender<TTestHost>(this ContainerBuilder builder, object key = null)
            where TTestHost : ITestHost
        {
            var testHost = Activator.CreateInstance<TTestHost>();

            var commandSenderRegistration = builder.Register(context => new HttpCommandSender(
                context.Resolve<ILogger>(),
                testHost.CreateClient(),
                new Uri("http://command-sender-mock.com"),
                context.Resolve<IOptions<HttpRequestOptions>>()));
            var queryenderRegistration = builder.Register(context => new HttpQuerySender(
                 context.Resolve<ILogger>(),
                 testHost.CreateClient(),
                 new Uri("http://command-sender-mock.com"),
                 context.Resolve<IOptions<HttpRequestOptions>>()));

            if (key == default) {
                commandSenderRegistration.As<ISendHttpCommand>();
                queryenderRegistration.As<ISendHttpQuery>();
            }
            else {
                commandSenderRegistration.Keyed<ISendHttpCommand>(key);
                queryenderRegistration.Keyed<ISendHttpQuery>(key);
            }
        }
    }
}
