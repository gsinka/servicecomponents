using System.Reflection;
using Autofac;
using FluentValidation;

namespace ServiceComponents.Infrastructure.Validation
{
    public static class ValidationAutofacExtensions
    {
        /// <summary>
        /// Add validation behavior
        /// </summary>
        /// <param name="builder">Container builder</param>
        /// <param name="assemblies">Assemblies to scan for validators</param>
        /// <returns></returns>
        public static ContainerBuilder AddValidationBehavior(this ContainerBuilder builder, params Assembly[] assemblies)
        {
            builder.RegisterGeneric(typeof(CommandValidationBehavior<>)).AsImplementedInterfaces().InstancePerDependency();
            builder.RegisterGeneric(typeof(QueryValidationBehavior<,>)).AsImplementedInterfaces().InstancePerDependency();

            builder.RegisterAssemblyTypes(assemblies).Where(t => t.IsClosedTypeOf(typeof(IValidator<>))).AsImplementedInterfaces().InstancePerDependency();

            return builder;
        }
    }
}