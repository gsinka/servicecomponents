using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Application.Mediator;

namespace ServiceComponents.Infrastructure.Behaviors.CommandConstraints
{
    public class CommandExecutionConstraintBehavior : IPreHandleCommand, IPostHandleCommand, IHandleCommandFailure
    {
        private readonly IDistributedCache _cache;
        private readonly Func<ICommand, string> _keyBuilder;
        private readonly Func<ICommand, string, (bool, string, TimeSpan?)> _constraint;

        public CommandExecutionConstraintBehavior(IDistributedCache cache, Func<ICommand, string> keyBuilder, Func<ICommand, string, (bool, string, TimeSpan?)> constraint)
        {
            _cache = cache;
            _keyBuilder = keyBuilder;
            _constraint = constraint;
        }

        public async Task PreHandleAsync(ICommand command, CancellationToken cancellationToken = default)
        {
            var key = _keyBuilder(command);

            if (key == default) return;

            var value = await _cache.GetStringAsync(key, cancellationToken);
            
            (var canExecute, var newValue, TimeSpan? expiration) = _constraint(command, value);

            if (canExecute) {
                if (string.IsNullOrWhiteSpace(newValue)) throw new ArgumentException("Command execution constraint value cannot be empty", nameof(newValue));
                await _cache.SetStringAsync(key, newValue, new DistributedCacheEntryOptions() { AbsoluteExpirationRelativeToNow = (expiration ?? TimeSpan.FromMinutes(1)) }, cancellationToken);
            }
            else {
                throw new CommandConstraintException("Cannot execute command because of constraints");
            }
        }

        public async Task PostHandleAsync(ICommand command, CancellationToken cancellationToken = default)
        {
            var key = _keyBuilder(command);
            if (key == default) return;
            await _cache.RemoveAsync(key, cancellationToken);
        }

        public async Task HandleFailureAsync(ICommand command, Exception exception, CancellationToken cancellationToken = default)
        {
            if (exception is CommandConstraintException) return;

            var key = _keyBuilder(command);
            if (key == default) return;
            await _cache.RemoveAsync(key, cancellationToken);
        }
    }

    public class CommandConstraintException : InvalidOperationException
    {
        public CommandConstraintException()
        {
        }

        protected CommandConstraintException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public CommandConstraintException(string message) : base(message)
        {
        }

        public CommandConstraintException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
