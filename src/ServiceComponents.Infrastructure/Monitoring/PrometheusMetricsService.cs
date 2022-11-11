using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Prometheus;
using ServiceComponents.Application.Monitoring;
using ServiceComponents.Core.Extensions;
using ServiceComponents.Core.Services;

namespace ServiceComponents.Infrastructure.Monitoring
{
    public class PrometheusMetricsService : IMetricsService
    {
        private readonly ConcurrentDictionary<object, Counter> _counters = new();
        private readonly ConcurrentDictionary<object, Summary> _summaries = new();
        private readonly ConcurrentDictionary<object, Histogram> _histograms = new();
        private readonly ConcurrentDictionary<object, Gauge> _gauges = new();

        public void Increment(object metric, double increment = 1)
        {
            Type metricType = metric.GetType();
            var metricKind = metricType.GetCustomAttributes(true).FirstOrDefault();

            switch (metricKind) {
                
                case CounterMetric counter:

                    if (!_counters.ContainsKey(metricType)) {

                        var (name, title) = metric.MetricDescription();
                        _counters.TryAdd(metricType, Metrics.CreateCounter(name, title, new CounterConfiguration() { LabelNames = metric.MetricFields().ToArray() }));
                    }
                    _counters[metricType].WithLabels(metric.MetricValues().ToArray()).Inc(increment);
                    break;

                case GaugeMetric gauge:

                    if (!_gauges.ContainsKey(metricType)) {

                        var (name, title) = metric.MetricDescription();
                        _gauges.TryAdd(metricType, Metrics.CreateGauge(name, title, new GaugeConfiguration { LabelNames = metric.MetricFields().ToArray() }));
                    }
                    _gauges[metricType].WithLabels(metric.MetricValues().ToArray()).Inc(increment);
                    break;

                default:
                    throw new InvalidOperationException($"Increment cannot be used on metric annotated as {metricKind.DisplayName()}");
            }
        }
        
        public void Decrement(object metric, double decrement = 1)
        {
            Type metricType = metric.GetType();
            var metricKind = metricType.GetCustomAttributes(true).FirstOrDefault();

            switch (metricKind) {
                
                case GaugeMetric gauge:

                    if (!_gauges.ContainsKey(metricType)) {

                        var (name, title) = metric.MetricDescription();
                        _gauges.TryAdd(metricType, Metrics.CreateGauge(name, title, new GaugeConfiguration { LabelNames = metric.MetricFields().ToArray() }));
                    }
                    _gauges[metricType].WithLabels(metric.MetricValues().ToArray()).Dec(decrement);
                    break;

                default:
                    throw new InvalidOperationException($"Decrement cannot be used on metric annotated as {metricKind.DisplayName()}");
            }
        }
        
        public void Set(object metric, double target)
        {
            Type metricType = metric.GetType();
            var metricKind = metricType.GetCustomAttributes(true).FirstOrDefault();

            switch (metricKind) {
                
                case GaugeMetric gauge:

                    if (!_gauges.ContainsKey(metricType)) {

                        var (name, title) = metric.MetricDescription();
                        _gauges.TryAdd(metricType, Metrics.CreateGauge(name, title, new GaugeConfiguration { LabelNames = metric.MetricFields().ToArray() }));
                    }
                    _gauges[metricType].WithLabels(metric.MetricValues().ToArray()).Set(target);
                    break;

                default:
                    throw new InvalidOperationException($"Set cannot be used on metric annotated as {metricKind.DisplayName()}");
            }
        }

        public void Observe(object metric, double duration)
        {
            Type metricType = metric.GetType();
            var metricKind = metricType.GetCustomAttributes(true).FirstOrDefault();

            switch (metricKind) {
                
                case SummaryMetric summary:

                    if (!_summaries.ContainsKey(metricType)) {
                        var (name, title) = metric.MetricDescription();
                        _summaries.TryAdd(metricType, Metrics.CreateSummary(name, title, new SummaryConfiguration() { LabelNames = metric.MetricFields().ToArray() }));
                    }
                    _summaries[metricType].WithLabels(metric.MetricValues().ToArray()).Observe(duration);

                    break;

                case LinearHistogramMetric linearHistogram:

                    if (!_histograms.ContainsKey(metric.GetType())) {
                        var (name, title) = metric.MetricDescription();
                        _histograms.TryAdd(
                            metricType, 
                            Metrics.CreateHistogram(
                                name, 
                                title, 
                                new HistogramConfiguration {
                                    Buckets = Histogram.LinearBuckets(linearHistogram.Start, linearHistogram.Width, linearHistogram.Count)
                                }));
                    }
                    _histograms[metricType].Observe(duration);

                    break;
            
                case ExponentialHistogramMetric exponentialHistogram:

                    if (!_histograms.ContainsKey(metric.GetType())) {
                        var (name, title) = metric.MetricDescription();
                        _histograms.TryAdd(
                            metricType, 
                            Metrics.CreateHistogram(
                                name, 
                                title, 
                                new HistogramConfiguration {
                                    Buckets = Histogram.ExponentialBuckets(exponentialHistogram.Start, exponentialHistogram.Factor, exponentialHistogram.Count)
                                }));
                    }
                    _histograms[metricType].Observe(duration);

                    break;

                default:
                    throw new InvalidOperationException($"Observe cannot be used on metric annotated as {metricKind.DisplayName()}");
            }
        }

        public void Observe(object metric, double val, long count)
        {
            Type metricType = metric.GetType();
            var metricKind = metricType.GetCustomAttributes(true).FirstOrDefault();

            switch (metricKind) {
                
                case LinearHistogramMetric linearHistogram:

                    if (!_histograms.ContainsKey(metricType)) {
                        var (name, title) = metric.MetricDescription();
                        _histograms.TryAdd(
                            metricType,
                            Metrics.CreateHistogram(
                                name,
                                title,
                                new HistogramConfiguration {
                                    Buckets = Histogram.LinearBuckets(linearHistogram.Start, linearHistogram.Width, linearHistogram.Count)
                                }));
                    }
                    _histograms[metricType].WithLabels(metric.MetricValues().ToArray()).Observe(val, count);

                    break;

                case ExponentialHistogramMetric exponentialHistogram:

                    if (!_histograms.ContainsKey(metric.GetType())) {
                        var (name, title) = metric.MetricDescription();
                        _histograms.TryAdd(
                            metricType,
                            Metrics.CreateHistogram(
                                name,
                                title,
                                new HistogramConfiguration {
                                    Buckets = Histogram.LinearBuckets(exponentialHistogram.Start, exponentialHistogram.Factor, exponentialHistogram.Count)
                                }));
                    }
                    _histograms[metricType].WithLabels(metric.MetricValues().ToArray()).Observe(val, count);

                    break;

                default:
                    
                    throw new InvalidOperationException($"Observe cannot be used on metric annotated as {metricKind.DisplayName()}");
            }
        }
    }
}