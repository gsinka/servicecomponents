using System;
using FluentNHibernate.Mapping;

namespace ReferenceApplication.Application.TestProcess
{
    public class TestProcessEntity
    {
        public virtual Guid Id { get; set; }
        public virtual DateTime StartTime { get; set; }
        public virtual DateTime EventTime { get; set; }
    }

    public class TestProcessEntityMap : ClassMap<TestProcessEntity>
    {
        public TestProcessEntityMap()
        {
            Id(x => x.Id).GeneratedBy.Assigned();
            Map(x => x.StartTime).Not.Nullable();
            Map(x => x.EventTime).Nullable();
        }
    }
}
