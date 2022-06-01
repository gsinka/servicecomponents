using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ServiceComponents.Infrastructure.Rabbit.HealthCheck
{
    public class RabbitMQConsumerChecker : IHealthCheck
    {
        private readonly IEnumerable<IRabbitConsumer> _consumers;

        public RabbitMQConsumerChecker(IEnumerable<IRabbitConsumer> consumers)
        {
            _consumers = consumers;
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken)
        {
            var data = new Dictionary<string, object>();
            var result = HealthCheckResult.Healthy("All consumers are healthy", data: data);
            foreach (var consumer in _consumers) {
                cancellationToken.ThrowIfCancellationRequested();
                data.Add(consumer.ConsumerTag, consumer.IsLive ? "Healthy" : "Unhealthy");
                if (!consumer.IsLive) result = HealthCheckResult.Unhealthy("One or more consumer is not healthy", data: data);
            }
            return Task.FromResult(result);
        }
    }
}
