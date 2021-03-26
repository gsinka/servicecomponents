using FluentNHibernate.Mapping;

namespace ReferenceApplication.Application.TestProcess
{
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