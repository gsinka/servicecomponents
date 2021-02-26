using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Autofac;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using NHibernate;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Application.Senders;
using ServiceComponents.Infrastructure.Rabbit.Senders;

namespace ServiceComponents.Infrastructure.NHibernate
{
    public class BackgroundEventPublisher : BackgroundService
    {
        private readonly IPublishEvent _eventPublisher;
        private readonly Channel<OutgoingEventEntity> _channel;
        private readonly ISessionFactory _sessionFactory;

        public BackgroundEventPublisher(IPublishEvent eventPublisher, Channel<OutgoingEventEntity> channel, ISessionFactory sessionFactory)
        {
            _eventPublisher = eventPublisher;
            _channel = channel;
            _sessionFactory = sessionFactory;
        }

        override protected async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (await _channel.Reader.WaitToReadAsync(stoppingToken)) {

                if (_channel.Reader.TryRead(out var outgoingEvent)) {

                    try {

                        await _eventPublisher.PublishAsync((IEvent)JsonConvert.DeserializeObject(outgoingEvent.EventPayload, Type.GetType(outgoingEvent.EventType)), stoppingToken);

                    }
                    catch (Exception exception) {

                        //await _channel.Writer.WriteAsync(outgoingEvent, CancellationToken.None);
                    }
                }
            }
        }
    }

    public static class RabbitNhibernatePublisherExtensions
    {
        public static ContainerBuilder AddNhibernateRabbitPublisher(this ContainerBuilder builder, string key = default)
        {
            builder.RegisterInstance(Channel.CreateUnbounded<OutgoingEventEntity>()).As<Channel<OutgoingEventEntity>>().SingleInstance();
            builder.Register(context => new BackgroundEventPublisher(context.Resolve<IPublishEvent>(), context.Resolve<Channel<OutgoingEventEntity>>(), context.Resolve<ISessionFactory>())).SingleInstance().As<IHostedService>();
            builder.RegisterDecorator<NHibernateEventPublisher, IPublishRabbitEvent>();

            return builder;
        }
    }
}
