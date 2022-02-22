using System;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceComponents.Domain
{
    public static class EventSourcedAggregateRootExtensions
    {
        public static async Task Execute<T>(this IEventSourceAggregateRepository repository, string aggregateId, Action<IEventSourceAggregateRoot> action, CancellationToken cancellationToken) where T : IEventSourceAggregateRoot
        {
            // Get aggregate from repository
            var aggregate = await repository.GetByIdAsync(aggregateId, cancellationToken);

            // Execute aggregate action
            action(aggregate);
            
            // Save aggregate
            await repository.SaveAsync(aggregate, cancellationToken);
        }
    }
}