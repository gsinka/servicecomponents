using System;
using System.Diagnostics;
using ServiceComponents.Application.Monitoring;

namespace ServiceComponents.Infrastructure.Monitoring
{
    public class DurationMetric<T> : IDisposable
    {
        private readonly IMetricsService _service;
        private readonly T _metric;
        private readonly Stopwatch _stopwatch = new();

        public DurationMetric(IMetricsService service, T metric)
        {
            _service = service;
            _metric = metric;
            _stopwatch.Start();
        }

        public void Dispose()
        {
            _stopwatch.Stop();
            _service.Observe(_metric, _stopwatch.ElapsedMilliseconds);
        }
    }
}