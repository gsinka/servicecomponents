using System;

namespace ServiceComponents.Core
{
    public interface IClock
    {
        DateTime UtcNow { get; }
    }
}
