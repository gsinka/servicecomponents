using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace ServiceComponents.AspNet.HealthCheck
{
    public static class HealthChecksBuilderExtensions
    {
        public static IHealthChecksBuilder AddConsumers(this IHealthChecksBuilder builder, string name, IEnumerable<string> tags)
        {
            return builder.AddCheck<RabbitMQConsumerChecker>(name, tags: tags);
        }
    }
}
