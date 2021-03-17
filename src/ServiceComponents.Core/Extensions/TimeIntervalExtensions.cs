using System;
using System.Collections.Generic;
using System.Linq;

namespace ServiceComponents.Core.Extensions
{
    public static class TimeIntervalExtensions
    {
        public static bool IntersectsWith(this ITimeInterval interval, ITimeInterval other)
        {
            return interval.Finish > other.Start && interval.Start < other.Finish;
        }

        public static bool Contains(this ITimeInterval interval, DateTime dateTime)
        {
            return interval.Start >= dateTime && interval.Finish > dateTime;
        }

        public static bool Contains(this ITimeInterval interval, ITimeInterval other)
        {
            return other.Start >= interval.Start && other.Finish <= interval.Finish;
        }

        public static bool StartsBefore(this ITimeInterval interval, ITimeInterval other)
        {
            return interval.Start < other.Start;
        }

        public static IEnumerable<ITimeInterval> Simplify(this IEnumerable<ITimeInterval> source)
        {
            var timeIntervals = source as ITimeInterval[] ?? source.ToArray();

            if (!timeIntervals.Any()) yield break;

            if (timeIntervals.Count() == 1)
            {
                yield return timeIntervals.First();
                yield break;
            }

            var changes = timeIntervals
                .SelectMany(x => new[] {new {d = x.Start, c = 1}, new {d = x.Finish, c = -1}})
                .OrderBy(x => x.d).ToList();
            
            var first = changes.FirstOrDefault();
            
            DateTime? recentStart = first.d;
            var counter = first.c;

            foreach (var change in changes.Skip(1))
            {
                counter += change.c;

                if (counter == 0)
                {
                    yield return new TimeInterval(recentStart.Value, change.d);
                    recentStart = null;
                }
                else if (change.c > 0 && !recentStart.HasValue)
                {
                    recentStart = change.d;
                }
            }
        }
    }
}