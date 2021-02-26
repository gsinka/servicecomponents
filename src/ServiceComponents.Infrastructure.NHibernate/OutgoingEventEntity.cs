using System;
using FluentNHibernate.Mapping;

namespace ServiceComponents.Infrastructure.NHibernate
{
    public class OutgoingEventEntity
    {
        public virtual string EventId { get; set; }
        public virtual string EventType { get; set; }
        public virtual string EventPayload { get; set; }
        public virtual DateTime TimeStamp { get; set; }
        public virtual int RetryCount { get; set; }
    }

    public class OutgoingEventEntityMap : ClassMap<OutgoingEventEntity>
    {
        public OutgoingEventEntityMap()
        {
            Table("outgoingevents");

            Id(x => x.EventId).GeneratedBy.Assigned();
            Map(x => x.EventType);
            Map(x => x.EventPayload);
            Map(x => x.TimeStamp);
            Map(x => x.RetryCount);
        }
    }
}