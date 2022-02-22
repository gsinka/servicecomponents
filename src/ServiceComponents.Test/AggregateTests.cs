using System;
using System.Collections.Generic;
using System.Text;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Domain;
using Xunit;

namespace ServiceComponents.Test
{
    public class AggregateTests
    {
        [Fact]
        public void Create_new_aggregate()
        {
            var ar = new TestAR("test");
            Assert.True(ar.Flag);
            Assert.Equal(1, ar.Version);

            ar.Set2();
            Assert.True(ar.Flag2);
            Assert.Equal(2, ar.Version);
            Assert.Equal(2, ((IEventSourceAggregateRoot)ar).GetUncommittedEvents().Count);
        }

        [Fact]
        public void Load_aggregate_from_events()
        {
            var ar = new TestAR(new []{ new TestEvent() });
            Assert.True(ar.Flag);
            Assert.Equal(1, ar.Version);
        }
    }

    public class TestAR : EventSourcedAggregateRoot
    {
        public bool Flag = false;
        public bool Flag2 = false;


        public TestAR(string aggregateId) : base(aggregateId)
        {
            RaiseEvent(new TestEvent());
        }

        public TestAR(IEnumerable<IEvent> events) : base(events)
        {
        }

        public void Set2()
        {
            RaiseEvent(new TestEvent2());
        }

        private void Apply(TestEvent evnt)
        {
            Flag = true;
        }

        private void Apply(TestEvent2 evnt)
        {
            Flag2 = true;
        }
    }

    public class TestEvent : Event
    {
        public TestEvent() : base(default) { }
    }

    public class TestEvent2 : Event
    {
        public TestEvent2() : base(default) { }
    }

}
