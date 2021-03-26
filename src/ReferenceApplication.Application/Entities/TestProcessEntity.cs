using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentNHibernate.Mapping;
using NHibernate;
using ServiceComponents.Application.ProcessManagement;

namespace ReferenceApplication.Application.Entities
{
    public class TestProcessEntity
    {
        public virtual Guid Id { get; set; }
        public virtual DateTime StartTime { get; set; }
        public virtual DateTime EventTime { get; set; }
        public virtual DateTime CloseTime { get; set; }
    }

    public class TestProcessEntityMap : ClassMap<TestProcessEntity>
    {
        public TestProcessEntityMap()
        {
            Id(x => x.Id).GeneratedBy.Assigned();
            Map(x => x.StartTime).Not.Nullable();
            Map(x => x.EventTime).Nullable();
            Map(x => x.CloseTime).Nullable();
        }
    }

    public class GenericNHibernateRepository<T> : IProcessRepository<T>
    {
        private readonly ISession _session;

        public GenericNHibernateRepository(ISession session)
        {
            _session = session;
        }

        public async Task<T> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _session.GetAsync<T>(id, cancellationToken);
        }

        public async Task Save(T process, CancellationToken cancellationToken = default)
        {
            using var tx = _session.BeginTransaction();
            await _session.SaveOrUpdateAsync(process, cancellationToken);
            await tx.CommitAsync(cancellationToken);
        }
    }
}
