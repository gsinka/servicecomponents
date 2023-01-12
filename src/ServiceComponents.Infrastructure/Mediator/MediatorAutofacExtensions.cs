using System.Reflection;
using Autofac;
using ServiceComponents.Application.Mediator;
using ServiceComponents.Infrastructure.Behaviors.Logging;
using ServiceComponents.Infrastructure.Dispatchers;
using ServiceComponents.Infrastructure.Receivers;

namespace ServiceComponents.Infrastructure.Mediator
{
    public static class MediatorAutofacExtensions
    {
        /// <summary>
        /// Add mediator
        /// </summary>
        /// <remarks>Registers dispatchers and handlers for commands, queries and events</remarks>
        /// <param name="builder">Container builder</param>
        /// <param name="assemblies">Assemblies to scan for command, query and event handlers</param>
        /// <returns></returns>
        public static ContainerBuilder AddMediator(this ContainerBuilder builder, params Assembly[] assemblies)
        {
            builder.RegisterType<CommandDispatcher>().As<IDispatchCommand>().InstancePerLifetimeScope();
            builder.RegisterType<QueryDispatcher>().As<IDispatchQuery>().InstancePerLifetimeScope();
            builder.RegisterType<EventDispatcher>().As<IDispatchEvent>().InstancePerLifetimeScope();

            builder.RegisterAssemblyTypes(assemblies).AsClosedTypesOf(typeof(IHandleCommand<>)).AsImplementedInterfaces().InstancePerDependency();
            builder.RegisterAssemblyTypes(assemblies).AsClosedTypesOf(typeof(IHandleQuery<,>)).AsImplementedInterfaces().InstancePerDependency();
            builder.RegisterAssemblyTypes(assemblies).AsClosedTypesOf(typeof(IHandleEvent<>)).AsImplementedInterfaces().InstancePerDependency();

            return builder;
        }

        /// <summary>
        /// Adds mediator pre, post and failure behaviors
        /// </summary>
        /// <param name="builder">Container builder</param>
        /// <param name="assemblies">Assemblies to scan for pre, post and failure handlers</param>
        /// <returns></returns>
        public static ContainerBuilder AddMediatorBehavior(this ContainerBuilder builder, params Assembly[] assemblies)
        {
            builder.RegisterDecorator<CommandDispatchBehavior, IDispatchCommand>();
            builder.RegisterDecorator<QueryDispatchBehavior, IDispatchQuery>();
            builder.RegisterDecorator<EventDispatchBehavior, IDispatchEvent>();

            builder.RegisterAssemblyTypes(assemblies).Where(type => type.IsAssignableTo<IPreHandleCommand>()).AsImplementedInterfaces().InstancePerDependency();
            builder.RegisterAssemblyTypes(assemblies).Where(type => type.IsAssignableTo<IPreHandleQuery>()).AsImplementedInterfaces().InstancePerDependency();
            builder.RegisterAssemblyTypes(assemblies).Where(type => type.IsAssignableTo<IPreHandleEvent>()).AsImplementedInterfaces().InstancePerDependency();

            builder.RegisterAssemblyTypes(assemblies).Where(type => type.IsAssignableTo<IPostHandleCommand>()).AsImplementedInterfaces().InstancePerDependency();
            builder.RegisterAssemblyTypes(assemblies).Where(type => type.IsAssignableTo<IPostHandleQuery>()).AsImplementedInterfaces().InstancePerDependency();
            builder.RegisterAssemblyTypes(assemblies).Where(type => type.IsAssignableTo<IPostHandleEvent>()).AsImplementedInterfaces().InstancePerDependency();

            builder.RegisterAssemblyTypes(assemblies).Where(type => type.IsAssignableTo<IHandleCommandFailure>()).AsImplementedInterfaces().InstancePerDependency();
            builder.RegisterAssemblyTypes(assemblies).Where(type => type.IsAssignableTo<IHandleQueryFailure>()).AsImplementedInterfaces().InstancePerDependency();
            builder.RegisterAssemblyTypes(assemblies).Where(type => type.IsAssignableTo<IHandleEventFailure>()).AsImplementedInterfaces().InstancePerDependency();

            builder.RegisterAssemblyTypes(assemblies).AsClosedTypesOf(typeof(IPreHandleCommand<>)).InstancePerDependency();
            builder.RegisterAssemblyTypes(assemblies).AsClosedTypesOf(typeof(IPreHandleQuery<,>)).InstancePerDependency();
            builder.RegisterAssemblyTypes(assemblies).AsClosedTypesOf(typeof(IPreHandleEvent<>)).InstancePerDependency();

            builder.RegisterAssemblyTypes(assemblies).AsClosedTypesOf(typeof(IPostHandleCommand<>)).InstancePerDependency();
            builder.RegisterAssemblyTypes(assemblies).AsClosedTypesOf(typeof(IPostHandleQuery<,>)).InstancePerDependency();
            builder.RegisterAssemblyTypes(assemblies).AsClosedTypesOf(typeof(IPostHandleEvent<>)).InstancePerDependency();

            builder.RegisterAssemblyTypes(assemblies).AsClosedTypesOf(typeof(IHandleCommandFailure<>)).InstancePerDependency();
            builder.RegisterAssemblyTypes(assemblies).AsClosedTypesOf(typeof(IHandleQueryFailure<,>)).InstancePerDependency();
            builder.RegisterAssemblyTypes(assemblies).AsClosedTypesOf(typeof(IHandleEventFailure<>)).InstancePerDependency();

            return builder;
        }

        public static ContainerBuilder AddReceiverCorrelationLogEnricherBehavior(this ContainerBuilder builder)
        {
            builder.RegisterDecorator<CommandReceiverCorrelationEnricherBehavior, IReceiveCommand>();
            builder.RegisterDecorator<QueryReceiverCorrelationEnricherBehavior, IReceiveQuery>();
            builder.RegisterDecorator<EventReceiverCorrelationEnricherBehavior, IReceiveEvent>();

            return builder;
        }
    }
}
