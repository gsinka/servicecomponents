using System;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ReferenceApplication.Api;
using ReferenceApplication.Application;
using ServiceComponents.AspNet.Wireup;

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
                .AddRedis("localhost:6379")
                .AddRedisCommandRules((command, commands) => commands.All(x => x.GetType() != command.GetType()))

                // Rabbit


                .Build(args).Run();
        }
    }
}
