using System;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Serilog;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Application.Senders;
using ServiceComponents.Core.Extensions;

namespace ServiceComponents.Infrastructure.Senders
{
    public class CommandRouter : ISendCommand
    {
        private readonly ILogger _log;
        private readonly ILifetimeScope _scope;
        private readonly Func<ICommand, object> _keySelector;

        public CommandRouter(ILogger log, ILifetimeScope scope, Func<ICommand, object> keySelector)
        {
            _log = log;
            _scope = scope;
            _keySelector = keySelector;
        }

        public async Task SendAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default) where TCommand : ICommand
        {
            var key = _keySelector(command);
            var sender = _scope.ResolveKeyed<ISendCommand>(key);

            _log.Verbose("Routing {commandType} to {senderType} based on key '{routingKey}'", command.DisplayName(), sender.DisplayName(), key);

            await sender.SendAsync(command, cancellationToken);
        }
    }
}