using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using Autofac;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using NHibernate.Tool.hbm2ddl;
using ReferenceApplication.Api;
using ReferenceApplication.Application;
using ReferenceApplication.Application.Entities;
using Serilog;
using Serilog.Events;
using ServiceComponents.AspNet.Wireup;
using ServiceComponents.Infrastructure.Behaviors.CommandConstraints;
using ServiceComponents.Infrastructure.Monitoring;

namespace ReferenceApplication2.AspNet
{
    public class Program
    {
        public static void Main(string[] args)
        {
            new ServiceComponentsHostBuilder()
                .UseDefault(
                
                    new [] { typeof(TestCommand).Assembly },
                    new []{ typeof(TestCommandHandler).Assembly})

                // Use serilog for logging
                .UseSerilog((context, log) => log
                    .WriteTo.Console(LogEventLevel.Information)
                    .WriteTo.Seq("http://localhost:5341", LogEventLevel.Verbose)
                    .Enrich.FromLogContext()
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                    .MinimumLevel.Override("NHibernate", LogEventLevel.Warning)
                    .MinimumLevel.Verbose())

                // Add OpenApi
                .AddOpenApi(
                    (configuration, options) => options.SwaggerDoc("v1", new OpenApiInfo { Title = "ReferenceApplication2.AspNet", Version = "v1" }), 
                    (configuration, options) => options.SwaggerEndpoint("/swagger/v1/swagger.json", "ReferenceApplication2.AspNet v1"))

                // Health check
                .AddHealthCheck((configuration, check) => {
                    check.AddRabbitMQ(rabbitConnectionString: "amqp://localhost:5672");
                    check.AddRedis("localhost");
                })
                
                // Add http sender
                .AddHttpSender(new Uri("http://localhost:5000/api/generic"), "http")

                // Routing
                .AddCommandRouter(command => "loopback")
                .AddQueryRouter(query => "loopback")
                .AddEventRouter(evnt => "loopback")

                // Redis
                //.AddRedis(configuration => "localhost:6379")

                // Rabbit
                //.AddRabbit("amqp://guest:guest@localhost:5672", "test2", "test-queue", "test-exchange", retryIntervals: new[] { 1000, 3000, 5000 })
                .AddRabbit("amqp://guest:guest@localhost:5672", "test2", "test-queue", "test-exchange")

                .RegisterCallback(builder => { builder.AddNewtonsoftJson(options => options.UseCamelCasing(true)); })
                
                .AddRedisDistributedCache("localhost")
                
                .RegisterCallback((context, builder) => {

                    builder.AddRequestConstraints(request => request switch {

                        LongCommand longCommand => new []{ "test1", "test2" },
                        TestCommand testCommand => new[] { "test2" },
                        _ => default

                    },(key, count) => (key, count) switch {

                        ("test1", _) when count > 0 => false,
                        ("test2", _) when count > 0 => false,
                        (_, _) => true

                    }, (key) => TimeSpan.FromSeconds(30));
                })

                // NHibernate
                .AddNHibernate(
                    configuration => "Server=localhost; Port=5432; Database=ref-app; User Id=postgres; Password=postgres", 
                    map => map.FluentMappings.AddFromAssemblyOf<TestEntity>(),
                    configuration => new SchemaUpdate(configuration).Execute(true, true))
         
                .AddPrometheusMetrics()
                
                .AddBadge()

                .Build(args).Run();
        }
    }
}
