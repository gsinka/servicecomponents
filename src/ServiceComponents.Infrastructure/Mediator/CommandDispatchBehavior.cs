﻿using System;
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
                
                foreach (dynamic preHandler in (IEnumerable<dynamic>)_scope.Resolve(typeof(IEnumerable<>).MakeGenericType(typeof(IPreHandleCommand<>).MakeGenericType(command.GetType())))) {
                    await preHandler.PreHandleAsync((dynamic)command, cancellationToken);
                }
                
                // Inner

                await _next.DispatchAsync(command, cancellationToken);

                // Post

                foreach (dynamic postHandler in (IEnumerable<dynamic>)_scope.Resolve(typeof(IEnumerable<>).MakeGenericType(typeof(IPostHandleCommand<>).MakeGenericType(command.GetType())))) {
                    await postHandler.PostHandleAsync((dynamic)command, cancellationToken);
                }

                foreach (var postHandler in _scope.Resolve<IEnumerable<IPostHandleCommand>>()) {
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

                foreach (dynamic errorHandler in (IEnumerable<dynamic>)_scope.Resolve(typeof(IEnumerable<>).MakeGenericType(typeof(IHandleCommandFailure<>).MakeGenericType(command.GetType())))) {
                    await errorHandler.HandleFailureAsync((dynamic)command, exception, cancellationToken);
                }

                throw;
            }
        }
    }
}