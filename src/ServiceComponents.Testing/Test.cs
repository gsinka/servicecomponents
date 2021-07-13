using System;
using Xunit;
using Xunit.Abstractions;

namespace ServiceComponents.Testing
{
    public class Test<TTestHost> : IClassFixture<TTestHost>, IDisposable
        where TTestHost : class, ITestHost
    {
        public Guid Id { get; } = Guid.NewGuid();
        protected TTestHost TestHost { get; }

        public Test(TTestHost testHost, ITestOutputHelper outputHelper)
        {
            TestHost = testHost;
            TestHost.OutputHelper = outputHelper;
        }

        public virtual void Dispose()
        {
            TestHost.Dispose();
        }
    }
}
