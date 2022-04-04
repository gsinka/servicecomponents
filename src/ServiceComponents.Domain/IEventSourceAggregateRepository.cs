using System.Threading;
using System.Threading.Tasks;

namespace ServiceComponents.Domain
{
    public interface IEventSourceAggregateRepository
    {
        Task<IEventSourceAggregateRoot> GetByIdAsync(string aggregateId, CancellationToken cancellation);
        Task SaveAsync(IEventSourceAggregateRoot aggregateRoot, CancellationToken cancellationToken);
    }
    public interface IEventSourceAggregateRepository<T> where T : IEventSourceAggregateRoot
    {
        Task<T> GetByIdAsync(string aggregateId, CancellationToken cancellation);
        Task SaveAsync(T aggregateRoot, CancellationToken cancellationToken);
    }
}