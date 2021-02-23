using Autofac;
using ServiceComponents.Infrastructure.Dispatchers;
using ServiceComponents.Infrastructure.Mediator;

namespace ServiceComponents.Infrastructure.Behaviors.Stopwatch
{
    public static class StopwatchAutofacExtension
    {
        public static ContainerBuilder AddStopwatchBehavior(this ContainerBuilder builder)
        {
            builder.RegisterDecorator<CommandStopwatchBehavior, IDispatchCommand>();
            builder.RegisterDecorator<QueryStopwatchBehavior, IDispatchQuery>();
            builder.RegisterDecorator<EventStopwatchBehavior, IDispatchEvent>();

            return builder;
        }
    }
}