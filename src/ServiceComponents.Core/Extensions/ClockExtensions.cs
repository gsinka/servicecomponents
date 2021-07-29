using System;
using ServiceComponents.Core.Services;

namespace ServiceComponents.Core.Extensions
{
    public static class ClockExtensions
    {
        public static TimeSpan ElapsedSince(this IClock clock, DateTime time) => clock.UtcNow - time;

    }
}
