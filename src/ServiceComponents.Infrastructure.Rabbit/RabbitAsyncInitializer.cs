using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceComponents.Infrastructure.Rabbit;

internal sealed class RabbitAsyncInitializer : IDisposable, IAsyncDisposable
{
    private readonly IRabbitAsyncResource[] _resources;
    private readonly object _sync = new();
    private Task _initialization;
    private Task _disposal;
    private bool _disposed;

    public RabbitAsyncInitializer(IEnumerable<IRabbitAsyncResource> resources)
    {
        _resources = resources.OrderBy(resource => resource.Order).ToArray();
    }

    public Task InitializeAsync(CancellationToken cancellationToken)
    {
        lock (_sync) {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _initialization ??= InitializeCoreAsync(cancellationToken);
        }
    }

    private async Task InitializeCoreAsync(CancellationToken cancellationToken)
    {
        try {
            foreach (var resource in _resources) {
                cancellationToken.ThrowIfCancellationRequested();
                await resource.InitializeAsync(cancellationToken).ConfigureAwait(false);
            }

            cancellationToken.ThrowIfCancellationRequested();
        }
        catch (Exception initializationException) {
            try {
                await DisposeResourcesAsync().ConfigureAwait(false);
            }
            catch (Exception disposalException) {
                throw new AggregateException("RabbitMQ initialization and cleanup failed.",
                    initializationException, disposalException);
            }

            throw;
        }
    }

    public ValueTask DisposeAsync()
    {
        lock (_sync) {
            if (_disposed) {
                return new ValueTask(_disposal ?? Task.CompletedTask);
            }

            _disposed = true;
            _disposal = DisposeCoreAsync();
            return new ValueTask(_disposal);
        }
    }

    private async Task DisposeCoreAsync()
    {
        if (_initialization != null) {
            try {
                // Do not dispose a connection while a channel is still being opened on it.
                await _initialization.ConfigureAwait(false);
            }
            catch {
                // Initialization has already propagated the failure and cleaned up partial results.
            }
        }

        await DisposeResourcesAsync().ConfigureAwait(false);
    }

    private async Task DisposeResourcesAsync()
    {
        var errors = new List<Exception>();
        for (var i = _resources.Length - 1; i >= 0; i--) {
            try {
                await _resources[i].DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception exception) {
                errors.Add(exception);
            }
        }

        if (errors.Count != 0) {
            throw new AggregateException("Failed to dispose RabbitMQ resources.", errors);
        }
    }

    public void Dispose()
    {
        lock (_sync) {
            if (_disposed) {
                return;
            }

            if (_initialization != null && !_initialization.IsCompleted) {
                throw new InvalidOperationException(
                    "RabbitMQ initialization is still in progress. Dispose the container asynchronously.");
            }

            _disposed = true;
        }

        var errors = new List<Exception>();
        for (var i = _resources.Length - 1; i >= 0; i--) {
            try {
                _resources[i].Dispose();
            }
            catch (Exception exception) {
                errors.Add(exception);
            }
        }

        if (errors.Count != 0) {
            throw new AggregateException("Failed to dispose RabbitMQ resources.", errors);
        }
    }
}