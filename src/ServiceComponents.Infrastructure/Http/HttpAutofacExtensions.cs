using Autofac;

namespace ServiceComponents.Infrastructure.Http
{
    public static class HttpAutofacExtensions
    {
        public static ContainerBuilder AddHttpReceivers(this ContainerBuilder builder)
        {
            builder.RegisterType<HttpCommandReceiver>().As<IReceiveHttpCommand>().InstancePerLifetimeScope();
            builder.RegisterType<HttpQueryReceiver>().As<IReceiveHttpQuery>().InstancePerLifetimeScope();
            builder.RegisterType<HttpEventReceiver>().As<IReceiveHttpEvent>().InstancePerLifetimeScope();

            return builder;
        }

        public static ContainerBuilder AddHttpRequestParser(this ContainerBuilder builder)
        {
            builder.RegisterType<HttpRequestParser>().AsSelf().InstancePerDependency();

            return builder;
        }
    }
}