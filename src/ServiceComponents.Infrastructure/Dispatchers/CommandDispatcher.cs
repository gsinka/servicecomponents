using System;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Serilog;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Application.Mediator;
using ServiceComponents.Core.ExtensionMethods;
using TypeExtensions = ServiceComponents.Core.ExtensionMethods.TypeExtensions;

namespace ServiceComponents.Infrastructure.Dispatchers
{
    public class CommandDispatcher : IDispatchCommand
    {
        private readonly ILogger _log;
        private readonly ILifetimeScope _scope;

        public CommandDispatcher(ILogger log, ILifetimeScope scope)
        {
            _log = log;
            _scope = scope;
        }

        public async Task DispatchAsync<T>(T command, CancellationToken cancellationToken = default) where T : ICommand
        {
            dynamic handler = _scope.ResolveOptional(typeof(IHandleCommand<>).MakeGenericType(command.GetType())) ?? throw new InvalidOperationException($"No handler registered for {command.DisplayName()}");
            _log.Verbose("Dispatching {commandType} to {handlerType}", command.DisplayName(), TypeExtensions.DisplayName(handler));
            await handler.HandleAsync((dynamic)command, cancellationToken);
        }
    }
}
