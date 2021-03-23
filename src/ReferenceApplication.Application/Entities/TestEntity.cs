using FluentNHibernate.Mapping;

namespace ReferenceApplication.Application.Entities
{
    public class TestEntity
    {
        public virtual long Id { get; set; }
        public virtual string Name { get; set; }
    }

    public class TestEntityMap : ClassMap<TestEntity>
    {
        public TestEntityMap()
        {
            Id(x => x.Id).GeneratedBy.Identity();
            Map(x => x.Name);
        }
    }
}
