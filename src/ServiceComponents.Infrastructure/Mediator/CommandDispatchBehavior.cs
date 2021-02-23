using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Application.Mediator;
using ServiceComponents.Infrastructure.Dispatchers;

namespace ServiceComponents.Infrastructure.Mediator
{
    public class CommandDispatchBehavior : IDispatchCommand
    {
        private readonly ILifetimeScope _scope;
        private readonly IDispatchCommand _next;

        public CommandDispatchBehavior(ILifetimeScope scope, IDispatchCommand next)
        {
            _scope = scope;
            _next = next;
        }

        public async Task DispatchAsync<T>(T command, CancellationToken cancellationToken = default) where T : ICommand
        {
            try
            {
                // Pre

                foreach (var preHandler in _scope.Resolve<IEnumerable<IPreHandleCommand>>())
                {
                    await preHandler.PreHandleAsync(command, cancellationToken);
                }

                foreach (var preHandler in _scope.Resolve<IEnumerable<IPreHandleCommand<T>>>())
                {
                    await preHandler.PreHandleAsync(command, cancellationToken);
                }

                // Inner

                await _next.DispatchAsync(command, cancellationToken);

                // Post

                foreach (var postHandler in _scope.Resolve<IEnumerable<IPostHandleCommand<T>>>())
                {
                    await postHandler.PostHandleAsync(command, cancellationToken);
                }

                foreach (var postHandler in _scope.Resolve<IEnumerable<IPostHandleCommand>>())
                {
                    await postHandler.PostHandleAsync(command, cancellationToken);
                }
            }
            catch (Exception exception)
            {
                // Error

                foreach (var errorHandler in _scope.Resolve<IEnumerable<IHandleCommandFailure>>())
                {
                    await errorHandler.HandleFailureAsync(command, exception, cancellationToken);
                }

                foreach (var errorHandler in _scope.Resolve<IEnumerable<IHandleCommandFailure<T>>>())
                {
                    await errorHandler.HandleFailureAsync(command, exception, cancellationToken);
                }

                throw;
            }
        }
    }
}