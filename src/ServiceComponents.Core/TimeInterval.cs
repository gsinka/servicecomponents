using System;
using System.Collections.Generic;
using System.Globalization;

namespace ServiceComponents.Core
{
    /// <summary>
    /// Time interval
    /// </summary>
    public class TimeInterval : ValueObject, ITimeInterval
    {
        public DateTime Start { get; }
        public DateTime Finish { get; }

        public TimeInterval(DateTime start, DateTime finish)
        {
            if (start < finish)
            {
                Start = start;
                Finish = finish;
            }
            else
            {
                Start = finish;
                Finish = start;
            }
        }

        public TimeInterval(DateTime start, TimeSpan duration) : this(start, start + duration) { }

        public TimeInterval(TimeSpan duration, DateTime finish) : this(finish - duration, finish) { }

        public TimeInterval(DateTime start) : this(start, DateTime.MaxValue) { }

        public void Deconstruct(out DateTime start, out DateTime finish)
        {
            start = Start;
            finish = Finish;
        }

        public TimeSpan Duration => Finish - Start;

        protected override IEnumerable<object> GetAtomicValues()
        {
            return new object[] { Start, Finish };
        }

        public override string ToString() => $"{Start.ToString(CultureInfo.InvariantCulture)}-{Finish.ToString(CultureInfo.InvariantCulture)}";
    }
}
