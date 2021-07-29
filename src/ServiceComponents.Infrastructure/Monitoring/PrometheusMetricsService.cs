using System;
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
        private readonly IClock _clock;
        private readonly Dictionary<object, Counter> _counters = new();
        private readonly Dictionary<object, Summary> _summaries = new();
        private readonly Dictionary<object, Histogram> _histograms = new();
        private readonly Dictionary<object, Gauge> _gauges = new();

        public void Increment(object metric, double increment = 1)
        {
            var metricType = metric.GetType();
            var metricKind = metricType.GetCustomAttributes(true).FirstOrDefault();

            switch (metricKind) {
                
                case CounterMetric counter:

                    if (!_counters.ContainsKey(metric.GetType())) {

                        var (name, title) = metric.MetricDescription();
                        _counters.Add(metric.GetType(), Metrics.CreateCounter(name, title, new CounterConfiguration() { LabelNames = metric.MetricFields().ToArray() }));
                    }
                    _counters[metric.GetType()].WithLabels(metric.MetricValues().ToArray()).Inc(increment);
                    break;

                case GaugeMetric gauge:

                    if (!_gauges.ContainsKey(metric.GetType())) {

                        var (name, title) = metric.MetricDescription();
                        _gauges.Add(metric.GetType(), Metrics.CreateGauge(name, title, new GaugeConfiguration { LabelNames = metric.MetricFields().ToArray() }));
                    }
                    _gauges[metric.GetType()].WithLabels(metric.MetricValues().ToArray()).Inc(increment);
                    break;

                default:
                    throw new InvalidOperationException($"Increment cannot be used on metric annotated as {metricKind.DisplayName()}");
            }
        }
        
        public void Decrement(object metric, double decrement = 1)
        {
            var metricType = metric.GetType();
            var metricKind = metricType.GetCustomAttributes(true).FirstOrDefault();

            switch (metricKind) {
                
                case GaugeMetric gauge:

                    if (!_gauges.ContainsKey(metric.GetType())) {

                        var (name, title) = metric.MetricDescription();
                        _gauges.Add(metric.GetType(), Metrics.CreateGauge(name, title, new GaugeConfiguration { LabelNames = metric.MetricFields().ToArray() }));
                    }
                    _gauges[metric.GetType()].WithLabels(metric.MetricValues().ToArray()).Dec(decrement);
                    break;

                default:
                    throw new InvalidOperationException($"Decrement cannot be used on metric annotated as {metricKind.DisplayName()}");
            }
        }
        
        public void Set(object metric, double target)
        {
            var metricType = metric.GetType();
            var metricKind = metricType.GetCustomAttributes(true).FirstOrDefault();

            switch (metricKind) {
                
                case GaugeMetric gauge:

                    if (!_gauges.ContainsKey(metric.GetType())) {

                        var (name, title) = metric.MetricDescription();
                        _gauges.Add(metric.GetType(), Metrics.CreateGauge(name, title, new GaugeConfiguration { LabelNames = metric.MetricFields().ToArray() }));
                    }
                    _gauges[metric.GetType()].WithLabels(metric.MetricValues().ToArray()).Set(target);
                    break;

                default:
                    throw new InvalidOperationException($"Set cannot be used on metric annotated as {metricKind.DisplayName()}");
            }
        }

        public void Observe(object metric, double duration)
        {
            var metricType = metric.GetType();
            var metricKind = metricType.GetCustomAttributes(true).FirstOrDefault();

            switch (metricKind) {
                
                case SummaryMetric summary:

                    if (!_summaries.ContainsKey(metric.GetType())) {
                        var (name, title) = metric.MetricDescription();
                        _summaries.Add(metric.GetType(), Metrics.CreateSummary(name, title, new SummaryConfiguration() { LabelNames = metric.MetricFields().ToArray() }));
                    }
                    _summaries[metric.GetType()].WithLabels(metric.MetricValues().ToArray()).Observe(duration);

                    break;

                case LinearHistogramMetric linearHistogram:

                    if (!_histograms.ContainsKey(metric.GetType())) {
                        var (name, title) = metric.MetricDescription();
                        _histograms.Add(
                            metric.GetType(), 
                            Metrics.CreateHistogram(
                                name, 
                                title, 
                                new HistogramConfiguration {
                                    Buckets = Histogram.LinearBuckets(linearHistogram.Start, linearHistogram.Width, linearHistogram.Count)
                                }));
                    }
                    _histograms[metric.GetType()].Observe(duration);

                    break;
            
                case ExponentialHistogramMetric exponentialHistogram:

                    if (!_histograms.ContainsKey(metric.GetType())) {
                        var (name, title) = metric.MetricDescription();
                        _histograms.Add(
                            metric.GetType(), 
                            Metrics.CreateHistogram(
                                name, 
                                title, 
                                new HistogramConfiguration {
                                    Buckets = Histogram.ExponentialBuckets(exponentialHistogram.Start, exponentialHistogram.Factor, exponentialHistogram.Count)
                                }));
                    }
                    _histograms[metric.GetType()].Observe(duration);

                    break;

                default:
                    throw new InvalidOperationException($"Observe cannot be used on metric annotated as {metricKind.DisplayName()}");
            }
        }

        public void Observe(object metric, double val, long count)
        {
            var metricType = metric.GetType();
            var metricKind = metricType.GetCustomAttributes(true).FirstOrDefault();

            switch (metricKind) {
                
                case LinearHistogramMetric linearHistogram:

                    if (!_histograms.ContainsKey(metric.GetType())) {
                        var (name, title) = metric.MetricDescription();
                        _histograms.Add(
                            metric.GetType(),
                            Metrics.CreateHistogram(
                                name,
                                title,
                                new HistogramConfiguration {
                                    Buckets = Histogram.LinearBuckets(linearHistogram.Start, linearHistogram.Width, linearHistogram.Count)
                                }));
                    }
                    _histograms[metric.GetType()].WithLabels(metric.MetricValues().ToArray()).Observe(val, count);

                    break;

                case ExponentialHistogramMetric exponentialHistogram:

                    if (!_histograms.ContainsKey(metric.GetType())) {
                        var (name, title) = metric.MetricDescription();
                        _histograms.Add(
                            metric.GetType(),
                            Metrics.CreateHistogram(
                                name,
                                title,
                                new HistogramConfiguration {
                                    Buckets = Histogram.LinearBuckets(exponentialHistogram.Start, exponentialHistogram.Factor, exponentialHistogram.Count)
                                }));
                    }
                    _histograms[metric.GetType()].WithLabels(metric.MetricValues().ToArray()).Observe(val, count);

                    break;

                default:
                    
                    throw new InvalidOperationException($"Observe cannot be used on metric annotated as {metricKind.DisplayName()}");
            }
        }
    }
}