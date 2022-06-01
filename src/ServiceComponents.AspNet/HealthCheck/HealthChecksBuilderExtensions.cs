using System.Collections.Generic;
using ServiceComponents.Infrastructure.Rabbit.HealthCheck;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class HealthChecksBuilderExtensions
    {
        public static IHealthChecksBuilder AddConsumers(this IHealthChecksBuilder builder, string name, IEnumerable<string> tags)
        {
            return builder.AddCheck<RabbitMQConsumerChecker>(name, tags: tags);
        }
    }
}
