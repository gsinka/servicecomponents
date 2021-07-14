using System;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using ServiceComponents.Api.Mediator;
using ServiceComponents.AspNet.Http;
using ServiceComponents.AspNet.Http.Senders;
using ServiceComponents.Infrastructure.Behaviors.Logging;
using ServiceComponents.Infrastructure.Behaviors.Stopwatch;
using ServiceComponents.Infrastructure.CorrelationContext;
using ServiceComponents.Infrastructure.Mediator;
using ServiceComponents.Infrastructure.Monitoring;
using ServiceComponents.Infrastructure.Rabbit;
using ServiceComponents.Infrastructure.Receivers;
using ServiceComponents.Infrastructure.Receivers.Loopback;
using ServiceComponents.Infrastructure.Senders;
using ServiceComponents.Infrastructure.Validation;

namespace ServiceComponents.AspNet.Wireup
{
    public static class ServiceComponentsHostBuilderExtensions
    {
        public static ServiceComponentsHostBuilder AddMediator(this ServiceComponentsHostBuilder hostBuilder, Assembly[] applicationAssemblies, bool addBehavior = true)
        {
            return hostBuilder.RegisterCallback((context, containerBuilder) => {

                containerBuilder.AddMediator(applicationAssemblies);
                if (addBehavior) {
                    containerBuilder.AddMediatorBehavior(applicationAssemblies);
                }
            });
        }
        
        public static ServiceComponentsHostBuilder AddValidationBehavior(this ServiceComponentsHostBuilder hostBuilder, Assembly[] apiAssemblies)
        {
            return hostBuilder.RegisterCallback((context, containerBuilder) => { containerBuilder.AddValidationBehavior(apiAssemblies); });
        }
        
        public static ServiceComponentsHostBuilder AddLogBehavior(this ServiceComponentsHostBuilder hostBuilder)
        {
            return hostBuilder.RegisterCallback((context, containerBuilder) => { containerBuilder.AddLogBehavior(); });
        }
        
        public static ServiceComponentsHostBuilder AddStopwatchBehavior(this ServiceComponentsHostBuilder hostBuilder)
        {
            return hostBuilder.RegisterCallback((context, containerBuilder) => { containerBuilder.AddStopwatchBehavior(); });
        }
        
        public static ServiceComponentsHostBuilder AddCorrelation(this ServiceComponentsHostBuilder hostBuilder)
        {
            return hostBuilder.RegisterCallback((context, containerBuilder) => {
                
                containerBuilder.AddCorrelationInfo();
                containerBuilder.AddReceiverCorrelationLogEnricherBehavior();
                containerBuilder.AddHttpReceiverCorrelationBehavior();
                containerBuilder.AddHttpSenderCorrelationBehavior();
                containerBuilder.AddLoopbackReceiverCorrelationBehavior();
                containerBuilder.AddRabbitReceiverCorrelationBehavior();
                containerBuilder.AddRabbitSenderCorrelationBehavior();
            });
        }
        
        public static ServiceComponentsHostBuilder AddReceivers(this ServiceComponentsHostBuilder hostBuilder, bool http = true, bool loopback = true)
        {
            return hostBuilder.RegisterCallback((context, containerBuilder) => { 
                
                containerBuilder.AddReceivers();
                if (http) containerBuilder.AddHttpReceivers();
                if (loopback) containerBuilder.AddLoopbackReceivers();

            });
        }

        public static ServiceComponentsHostBuilder AddHttpSender(this ServiceComponentsHostBuilder hostBuilder, Func<IConfiguration, Uri> endpointUriBuilder, object key = default)
        {
            return hostBuilder.RegisterCallback((context, containerBuilder) => {

                var uri = endpointUriBuilder(context.Configuration);
                containerBuilder.AddHttpCommandSender(uri, key);
                containerBuilder.AddHttpQuerySender(uri, key);
                containerBuilder.AddHttpEventPublisher(uri, key);
            });
        }

        public static ServiceComponentsHostBuilder AddHttpSender(this ServiceComponentsHostBuilder hostBuilder, Uri endpointUri, object key = default)
        {
            return hostBuilder.RegisterCallback((context, containerBuilder) => {
                
                containerBuilder.AddHttpCommandSender(endpointUri, key);
                containerBuilder.AddHttpQuerySender(endpointUri, key);
                containerBuilder.AddHttpEventPublisher(endpointUri, key);
            });
        }

        public static ServiceComponentsHostBuilder AddLoopbackSender(this ServiceComponentsHostBuilder hostBuilder, object key = default)
        {
            return hostBuilder.RegisterCallback((context, containerBuilder) => {
                
                containerBuilder.AddLoopbackCommandSender(key);
                containerBuilder.AddLoopbackQuerySender(key);
                containerBuilder.AddLoopbackEventPublisher(key);
            });
        }

        public static ServiceComponentsHostBuilder AddCommandRouter(this ServiceComponentsHostBuilder hostBuilder, Func<ICommand, object> keySelectorFunc)
        {
            return hostBuilder.RegisterCallback((context, containerBuilder) => {
                containerBuilder.AddCommandRouter(keySelectorFunc);

            });
        }
        
        public static ServiceComponentsHostBuilder AddQueryRouter(this ServiceComponentsHostBuilder hostBuilder, Func<IQuery, object> keySelectorFunc)
        {
            return hostBuilder.RegisterCallback((context, containerBuilder) => {
                containerBuilder.AddQueryRouter(keySelectorFunc);

            });
        }

        public static ServiceComponentsHostBuilder AddEventRouter(this ServiceComponentsHostBuilder hostBuilder, Func<IEvent, object> keySelectorFunc)
        {
            return hostBuilder.RegisterCallback((context, containerBuilder) => {
                containerBuilder.AddEventRouter(keySelectorFunc);

            });
        }

        public static ServiceComponentsHostBuilder AddPrometheusMetrics(this ServiceComponentsHostBuilder hostBuilder)
        {
            return hostBuilder.RegisterCallback((context, containerBuilder) => {
                containerBuilder.AddPrometheusRequestMetricsService();
                containerBuilder.AddPrometheusRequestMetricsBehavior();

            });
        }

    }
}