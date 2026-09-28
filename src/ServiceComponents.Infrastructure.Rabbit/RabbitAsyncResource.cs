using System;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceComponents.Infrastructure.Rabbit;

internal interface IRabbitAsyncResource : IDisposable, IAsyncDisposable
{
    int Order { get; }
    Task InitializeAsync(CancellationToken cancellationToken);
}

internal sealed class RabbitAsyncResource<T> : IRabbitAsyncResource
    where T : class, IDisposable, IAsyncDisposable
{
    private readonly Func<CancellationToken, Task<T>> _factory;
    private T _value;

    public RabbitAsyncResource(Func<CancellationToken, Task<T>> factory, int order)
    {
        _factory = factory;
        Order = order;
    }

    public int Order { get; }

    public T Value => Volatile.Read(ref _value) ?? throw new InvalidOperationException(
        $"RabbitMQ {typeof(T).Name} is not initialized. Await scope.InitializeRabbitAsync() after Build() " +
        "and before resolving RabbitMQ services. Do not resolve them from IStartable or build callbacks.");

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        var value = await _factory(cancellationToken).ConfigureAwait(false);
        if (value == null) {
            throw new InvalidOperationException($"The RabbitMQ factory returned no {typeof(T).Name}.");
        }

        Volatile.Write(ref _value, value);
    }

    public void Dispose()
    {
        Interlocked.Exchange(ref _value, null)?.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        var value = Interlocked.Exchange(ref _value, null);
        if (value != null) {
            await value.DisposeAsync().ConfigureAwait(false);
        }
    }
}