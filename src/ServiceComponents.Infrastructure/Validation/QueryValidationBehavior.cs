using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using FluentValidation;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Application.Mediator;

namespace ServiceComponents.Infrastructure.Validation
{
    public class QueryValidationBehavior<TQuery, TResult> : IPreHandleQuery<TQuery, TResult> where TQuery : IQuery<TResult>
    {
        private readonly ILifetimeScope _scope;

        public QueryValidationBehavior(ILifetimeScope scope)
        {
            _scope = scope;
        }

        public async Task PreHandleAsync(TQuery query, CancellationToken cancellationToken = default)
        {
            foreach (var validator in (IEnumerable<IValidator>)_scope.Resolve(typeof(IEnumerable<>).MakeGenericType(typeof(IValidator<>).MakeGenericType(query.GetType()))))
            {
                await DefaultValidatorExtensions.ValidateAndThrowAsync((dynamic)validator, (dynamic)query, cancellationToken);
            }
        }
    }
}