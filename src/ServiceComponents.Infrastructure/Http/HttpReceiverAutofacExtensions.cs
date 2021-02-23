using Autofac;

namespace ServiceComponents.Infrastructure.Http
{
    public static class HttpReceiverAutofacExtensions
    {
        public static ContainerBuilder AddHttpReceiverCorrelationBehavior(this ContainerBuilder builder)
        {
            builder.RegisterDecorator<HttpCommandReceiverCorrelationBehavior, IReceiveHttpCommand>();
            builder.RegisterDecorator<HttpQueryReceiverCorrelationBehavior, IReceiveHttpQuery>();
            builder.RegisterDecorator<HttpEventReceiverCorrelationBehavior, IReceiveHttpEvent>();

            return builder;
        }
    }
}