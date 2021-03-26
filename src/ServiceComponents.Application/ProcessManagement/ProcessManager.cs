using System;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceComponents.Application.ProcessManagement
{
    public interface IProcessRepository<T>
    {
        Task<T> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task Save(T process, CancellationToken cancellationToken = default);
    }

}
