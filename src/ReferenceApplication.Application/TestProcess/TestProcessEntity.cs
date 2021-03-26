using System;

namespace ReferenceApplication.Application.TestProcess
{
    public class TestProcessEntity
    {
        public virtual Guid Id { get; set; }
        public virtual DateTime StartTime { get; set; }
        public virtual DateTime EventTime { get; set; }
    }
}
