using System.ComponentModel;
using System.Linq;
using ReferenceApplication.Api;
using ServiceComponents.Infrastructure.Monitoring;
using Xunit;

namespace ServiceComponents.Test
{
    public class MetricTests
    {
        [Fact]
        public void TestFieldName()
        {
            var metric = new RequestCounterMetric(new TestCommand("data"));

            var fields = metric.MetricFields().ToArray();
            
            Assert.Equal(2, fields.Count());
            Assert.Equal("namespace", fields[0]);
            Assert.Equal("name", fields[1]);



        }
    }
}
