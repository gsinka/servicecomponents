using System;

namespace ServiceComponents.Core
{
    public class ComputerClock : IClock
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}