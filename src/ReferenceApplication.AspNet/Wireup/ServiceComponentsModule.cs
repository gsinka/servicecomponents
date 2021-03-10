using System;
using Autofac;
using Microsoft.Extensions.Configuration;
using ReferenceApplication.Api;
using ReferenceApplication.Application;
using ServiceComponents.AspNet.Http;
using ServiceComponents.AspNet.Http.Senders;
using ServiceComponents.Core;
using ServiceComponents.Infrastructure.Behaviors.Logging;
using ServiceComponents.Infrastructure.Behaviors.Stopwatch;
using ServiceComponents.Infrastructure.CorrelationContext;
using ServiceComponents.Infrastructure.Mediator;
using ServiceComponents.Infrastructure.Receivers;
using ServiceComponents.Infrastructure.Redis;
using ServiceComponents.Infrastructure.Senders;
using ServiceComponents.Infrastructure.Validation;

namespace ReferenceApplication.AspNet.Wireup
{
    public class ServiceComponentsModule : Module
    {
        private readonly IConfiguration _configuration;

        public ServiceComponentsModule(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        protected override void Load(ContainerBuilder builder)
        {
            var apiAssembly = typeof(TestCommandValidator).Assembly;
            var applicationAssembly = typeof(TestCommandHandler).Assembly;

            builder.RegisterType<ComputerClock>().AsImplementedInterfaces().SingleInstance();

            builder.AddMediator(applicationAssembly);
            builder.AddMediatorBehavior(applicationAssembly);
            builder.AddValidationBehavior(apiAssembly);
            builder.AddLogBehavior();
            builder.AddStopwatchBehavior();

            builder.AddCorrelationInfo();

            builder.AddReceivers();
            builder.AddReceiverCorrelationLogEnricherBehavior();

            builder.AddHttpRequestParser();
            builder.AddHttpReceivers();
            builder.AddHttpReceiverCorrelationBehavior();

            builder.AddLoopbackReceivers();
            builder.AddLoopbackReceiverCorrelationBehavior();

            builder.AddCommandRouter(command => "loopback");

            builder.AddHttpCommandSender(new Uri("http://localhost:5000/api/generic"), "http");
            builder.AddLoopbackCommandSender("loopback");
            
            builder.AddQueryRouter(query => "loopback");
            builder.AddHttpQuerySender(new Uri("http://localhost:5000/api/generic"), "http");
            builder.AddLoopbackQuerySender("loopback");

            builder.AddEventRouter(@event => "rabbit");
            builder.AddHttpEventPublisher(new Uri("http://localhost:5000/api/generic"), "rabbit");
            builder.AddLoopbackEventPublisher("loopback");

            builder.AddHttpSenderCorrelationBehavior();

            builder.AddRedis(_configuration.GetValue("connectionStrings:redis", "localhost"));

        }
    }
}
