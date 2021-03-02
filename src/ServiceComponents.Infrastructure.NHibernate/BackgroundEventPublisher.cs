using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Autofac;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using NHibernate;
using NHibernate.Linq;
using NHibernate.Util;
using Npgsql.TypeHandlers;
using Serilog;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Application.Senders;
using ServiceComponents.Core.ExtensionMethods;

namespace ServiceComponents.Infrastructure.NHibernate
{
    public class BackgroundServicePublisherHost : BackgroundService
    {
        private readonly BackgroundEventPublisher _publisher;

        public BackgroundServicePublisherHost(BackgroundEventPublisher publisher)
        {
            _publisher = publisher;
        }

        override protected async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _publisher.ExecuteAsync(stoppingToken);
        }
    }

    public class BackgroundEventPublisher
    {
        private readonly ILogger _log;
        private readonly IPublishEvent _eventPublisher;
        private readonly ISessionFactory _sessionFactory;
        private bool _isExecuted = false;

        public BackgroundEventPublisher(ILogger log, IPublishEvent eventPublisher, ISessionFactory sessionFactory)
        {
            _log = log;
            _eventPublisher = eventPublisher;
            _sessionFactory = sessionFactory;
        }

        public async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_isExecuted) return;
            _isExecuted = true;

            while (!stoppingToken.IsCancellationRequested) {

                _log.Information("Starting republish");

                using (var session = _sessionFactory.OpenSession())
                using (var tx = session.BeginTransaction()) {

                    try {

                        var unpublishedEvents = await session.Query<OutgoingEventEntity>().WithLock(LockMode.Upgrade).ToListAsync(stoppingToken);

                        if (unpublishedEvents.Any()) {

                            foreach (var unpublished in unpublishedEvents) {

                                try {
                                    await _eventPublisher.PublishAsync((IEvent)JsonConvert.DeserializeObject(unpublished.EventPayload, Type.GetType(unpublished.EventType)), stoppingToken);
                                    await session.DeleteAsync(unpublished, stoppingToken);
                                    _log.Verbose("Republish of {eventType} with id '{eventId}' succeeded", unpublished.DisplayName(), unpublished.EventId);
                                }
                                catch (Exception exception) {
                                    _log.Error(exception, "Republish of {eventType} with id '{eventId}' failed", unpublished.DisplayName(), unpublished.EventId);
                                    unpublished.RetryCount++;
                                }
                            }

                            await tx.CommitAsync(stoppingToken);
                        }
                    }
                    catch (Exception exception) {

                        await tx.RollbackAsync(stoppingToken);
                        _log.Error("Republish failed", exception);
                    }

                }

                await Task.Delay(1000, stoppingToken);
            }
        }
    }

    public static class RabbitNhibernatePublisherExtensions
    {
        public static ContainerBuilder AddNhibernateRabbitPublisher(this ContainerBuilder builder, string key = default)
        {
            //builder.RegisterInstance(Channel.CreateUnbounded<IEnumerable<IEvent>>()).As<Channel<IEnumerable<IEvent>>>().SingleInstance();

            builder.RegisterType<BackgroundEventPublisher>().AsSelf().SingleInstance();
            builder.RegisterType<BackgroundServicePublisherHost>().AsImplementedInterfaces().InstancePerDependency();

            //builder.Register(context => new BackgroundEventPublisher(
            //        context.Resolve<ILogger>(),
            //        context.Resolve<IPublishEvent>(),
            //        context.Resolve<ISessionFactory>()))
            //    .SingleInstance().AsImplementedInterfaces();

            
            //builder.Register(context => context.Resolve<BackgroundEventPublisher>()).As<IHostedService>();

            //builder.RegisterDecorator<NHibernateEventPublisher, IPublishRabbitEvent>();

            return builder;
        }
    }
}
