using System;
using Autofac;
using ReferenceApplication.Api;
using ReferenceApplication.Application;
using ServiceComponents.Infrastructure.Behaviors.Logging;
using ServiceComponents.Infrastructure.Behaviors.Stopwatch;
using ServiceComponents.Infrastructure.CorrelationContext;
using ServiceComponents.Infrastructure.Http;
using ServiceComponents.Infrastructure.Mediator;
using ServiceComponents.Infrastructure.Receivers;
using ServiceComponents.Infrastructure.Senders;
using ServiceComponents.Infrastructure.Validation;

namespace ReferenceApplication.AspNet.Wireup
{
    public class ServiceComponentsModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            var apiAssembly = typeof(TestCommandValidator).Assembly;
            var applicationAssembly = typeof(TestCommandHandler).Assembly;

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

            
            builder.AddCommandRouter(command => "http");

            builder.AddHttpCommandSender(new Uri("http://localhost:5000/api/generic"), "http");
            
            builder.AddQueryRouter(command => "http");
            builder.AddHttpQuerySender(new Uri("http://localhost:5000/api/generic"), "http");

            builder.AddEventRouter(command => "rabbit");
            builder.AddHttpEventPublisher(new Uri("http://localhost:5000/api/generic"), "rabbit");

            builder.AddHttpSenderCorrelationBehavior();


        }
    }
}
