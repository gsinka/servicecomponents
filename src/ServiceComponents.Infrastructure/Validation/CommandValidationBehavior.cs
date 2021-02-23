using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using FluentValidation;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Application.Mediator;

namespace ServiceComponents.Infrastructure.Validation
{
    public class CommandValidationBehavior<T> : IPreHandleCommand<T> where T : ICommand
    {
        private readonly ILifetimeScope _scope;

        public CommandValidationBehavior(ILifetimeScope scope)
        {
            _scope = scope;
        }

        public async Task PreHandleAsync(T command, CancellationToken cancellationToken = default)
        {
            foreach (var validator in (IEnumerable<IValidator>)_scope.Resolve(typeof(IEnumerable<>).MakeGenericType(typeof(IValidator<>).MakeGenericType(command.GetType()))))
            {
                await DefaultValidatorExtensions.ValidateAndThrowAsync((dynamic)validator, (dynamic)command, cancellationToken);
            }
        }
    }
}
