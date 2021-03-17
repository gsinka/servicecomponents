using System;

namespace ServiceComponents.Core.Services
{
    public class ComputerClock : IClock
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}