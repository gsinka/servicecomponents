using System;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Infrastructure.Receivers;
using StackExchange.Redis;

namespace ServiceComponents.Infrastructure.Redis
{
    public static class RedisAutofacExtensions
    {
        public static ContainerBuilder AddRedis(this ContainerBuilder builder, string connection)
        {
            builder.Register(context => ConnectionMultiplexer.Connect(connection)).As<IConnectionMultiplexer>().SingleInstance();

            return builder;
        }
    }

    public class RedisReceiverRetryBehavior : IReceiveEvent
    {
        private readonly IReceiveEvent _next;
        private readonly IConnectionMultiplexer _connection;

        public RedisReceiverRetryBehavior(IReceiveEvent next, IConnectionMultiplexer connection)
        {
            _next = next;
            _connection = connection;
        }

        public async Task ReceiveAsync<T>(T @event, CancellationToken cancellationToken = default) where T : IEvent
        {

            try {
                await _next.ReceiveAsync(@event, cancellationToken);
            }
            catch (Exception exception) {

                var db = _connection.GetDatabase();
                await db.StringSetAsync(new RedisKey(@event.EventId), new RedisValue());
            }
        }
    }
}
