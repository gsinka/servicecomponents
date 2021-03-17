using System;

namespace ServiceComponents.Core.Services
{
    public interface IClock
    {
        DateTime UtcNow { get; }
    }
}
