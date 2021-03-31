using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Application.Mediator;

namespace ServiceComponents.Infrastructure.Behaviors.CommandConstraints
{
    public class RequestExecutionConstraintBehavior : 
        IPreHandleCommand, IPostHandleCommand, IHandleCommandFailure,
        IPreHandleQuery, IPostHandleQuery, IHandleQueryFailure

    {
        private readonly IDistributedCache _cache;
        private readonly Func<IRequest, string[]> _keys;
        private readonly Func<string, int, bool> _constraint;
        private readonly Func<string, TimeSpan?> _expiry;

        public RequestExecutionConstraintBehavior(IDistributedCache cache, Func<IRequest, string[]> keys, Func<string, int, bool> constraint, Func<string, TimeSpan?> expiry)
        {
            _cache = cache;
            _keys = keys;
            _constraint = constraint;
            _expiry = expiry;
        }

        // Command

        public async Task PreHandleAsync(ICommand command, CancellationToken cancellationToken = default)
        {
            await IncrementValue(command, cancellationToken);
        }

        public async Task PostHandleAsync(ICommand command, CancellationToken cancellationToken = default)
        {
            await DecrementValue(command, cancellationToken);
        }

        public async Task HandleFailureAsync(ICommand command, Exception exception, CancellationToken cancellationToken = default)
        {
            if (exception is CommandConstraintException) return;
            await DecrementValue(command, cancellationToken);
        }

        // Query

        public async Task PreHandleAsync(IQuery query, CancellationToken cancellationToken = default)
        {
            await IncrementValue(query, cancellationToken);
        }

        public async Task PostHandleAsync(IQuery query, object result, CancellationToken cancellationToken = default)
        {
            await DecrementValue(query, cancellationToken);
        }

        public async Task HandleFailureAsync(IQuery query, Exception exception, CancellationToken cancellationToken = default)
        {
            if (exception is CommandConstraintException) return;
            await DecrementValue(query, cancellationToken);
        }

        // Helpers

        private async Task IncrementValue(IRequest request, CancellationToken cancellationToken)
        {
            var keys = _keys(request);
            if (keys == default || keys.Length == 0) return;

            foreach (var key in keys)
            {
                var cacheItem = await _cache.GetAsync(key, cancellationToken);
                var value = cacheItem == null ? 0 : BitConverter.ToInt32(cacheItem, 0);

                if (_constraint(key, value))
                {
                    value++;
                    await _cache.SetAsync(key, BitConverter.GetBytes(value),
                        new DistributedCacheEntryOptions() {AbsoluteExpirationRelativeToNow = _expiry(key)}, cancellationToken);
                }
                else
                {
                    throw new CommandConstraintException("Cannot execute request because of constraints");
                }
            }
        }

        private async Task DecrementValue(IRequest request, CancellationToken cancellationToken)
        {
            var keys = _keys(request);
            if (keys == default || keys.Length == 0) return;

            foreach (var key in keys) {
                var value = BitConverter.ToInt32(await _cache.GetAsync(key, cancellationToken), 0);
                value--;
                if (value == 0) {
                    await _cache.SetAsync(key, BitConverter.GetBytes(value), cancellationToken);
                }
                else {
                    await _cache.RemoveAsync(key, cancellationToken);
                }
            }
        }
    }
}