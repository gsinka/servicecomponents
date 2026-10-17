using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Prometheus;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Application.Monitoring;
using ServiceComponents.Infrastructure.Monitoring;
using Xunit;

namespace ServiceComponents.Test;

/// <summary>
/// Pins what PrometheusMetricsService exports (metric names, label names, label values, values) and which
/// exceptions it throws. prometheus-net uses a process-wide registry, so every metric here has a unique name.
/// Must pass before and after the performance work.
/// </summary>
public class PrometheusMetricsCharacterizationTests
{
    [CounterMetric("characterization_counter", "Characterization counter")]
    public class SampleCounter
    {
        [MetricField("first_label")]
        public string First { get; }
        public string Second { get; }
        public int Number { get; }

        public SampleCounter(string first, string second, int number)
        {
            First = first;
            Second = second;
            Number = number;
        }
    }

    [GaugeMetric("characterization_gauge", "Characterization gauge")]
    public class SampleGauge
    {
        [MetricField("zone")]
        public string Zone { get; }

        public SampleGauge(string zone) => Zone = zone;
    }

    [SummaryMetric("characterization_summary", "Characterization summary")]
    public class SampleSummary
    {
        [MetricField("op")]
        public string Operation { get; }

        public SampleSummary(string operation) => Operation = operation;
    }

    [LinearHistogramMetric("characterization_linear_histogram", "Characterization linear histogram", 0, 10, 3)]
    public class SampleLinearHistogram
    {
    }

    [ExponentialHistogramMetric("characterization_exponential_histogram", "Characterization exponential histogram", 1, 2, 3)]
    public class SampleExponentialHistogram
    {
    }

    [LinearHistogramMetric("characterization_linear_histogram_counted", "Characterization linear histogram with count", 0, 5, 2)]
    public class SampleCountedHistogram
    {
    }

    [CounterMetric("characterization_counter_null_label", "Null label")]
    public class NullLabelCounter
    {
        public string Value { get; }

        public NullLabelCounter(string value) => Value = value;
    }

    [CounterMetric("characterization_counter_throwing_label", "Throwing label")]
    public class ThrowingLabelCounter
    {
        public string Value => throw new InvalidOperationException("getter failed");
    }

    public class UnannotatedMetric
    {
        public string Value => "x";
    }

    public class CharacterizationCommand : ICommand
    {
        public string CommandId => "metric-command";
    }

    private static async Task<string[]> Export(string metricName)
    {
        using var stream = new MemoryStream();
        await Metrics.DefaultRegistry.CollectAndExportAsTextAsync(stream);
        var text = System.Text.Encoding.UTF8.GetString(stream.ToArray());
        return text.Split('\n').Where(line => line.StartsWith(metricName + "{") || line.StartsWith(metricName + " ") || line.Contains(" " + metricName + " ")).ToArray();
    }

    [Fact]
    public async Task Counter_uses_metric_field_names_and_property_values_as_labels()
    {
        var service = new PrometheusMetricsService();

        service.Increment(new SampleCounter("a", "b", 1));
        service.Increment(new SampleCounter("a", "b", 1), 2.5);
        service.Increment(new SampleCounter("x", "y", 2));

        Assert.Equal(new[] {
            "# HELP characterization_counter Characterization counter",
            "# TYPE characterization_counter counter",
            "characterization_counter{first_label=\"a\",Second=\"b\",Number=\"1\"} 3.5",
            "characterization_counter{first_label=\"x\",Second=\"y\",Number=\"2\"} 1",
        }, await Export("characterization_counter"));
    }

    [Fact]
    public async Task Gauge_supports_increment_decrement_and_set()
    {
        var service = new PrometheusMetricsService();

        service.Increment(new SampleGauge("eu"), 5);
        service.Decrement(new SampleGauge("eu"), 2);
        service.Set(new SampleGauge("us"), 7);
        service.Decrement(new SampleGauge("us"));

        Assert.Equal(new[] {
            "# HELP characterization_gauge Characterization gauge",
            "# TYPE characterization_gauge gauge",
            "characterization_gauge{zone=\"eu\"} 3",
            "characterization_gauge{zone=\"us\"} 6",
        }, await Export("characterization_gauge"));
    }

    [Fact]
    public async Task Summary_observes_values_with_labels()
    {
        var service = new PrometheusMetricsService();

        service.Observe(new SampleSummary("read"), 4);
        service.Observe(new SampleSummary("read"), 6);

        var lines = await Export("characterization_summary");
        Assert.Contains("characterization_summary_sum{op=\"read\"} 10", lines.Concat(await Export("characterization_summary_sum")));
        Assert.Contains("characterization_summary_count{op=\"read\"} 2", lines.Concat(await Export("characterization_summary_count")));
    }

    [Fact]
    public async Task Histograms_use_buckets_from_the_attribute()
    {
        var service = new PrometheusMetricsService();

        service.Observe(new SampleLinearHistogram(), 12);
        service.Observe(new SampleExponentialHistogram(), 3);
        service.Observe(new SampleCountedHistogram(), 4, 3);

        Assert.Equal(new[] {
            "characterization_linear_histogram_bucket{le=\"0\"} 0",
            "characterization_linear_histogram_bucket{le=\"10\"} 0",
            "characterization_linear_histogram_bucket{le=\"20\"} 1",
            "characterization_linear_histogram_bucket{le=\"+Inf\"} 1",
        }, await Export("characterization_linear_histogram_bucket"));
        Assert.Equal(new[] {
            "characterization_exponential_histogram_bucket{le=\"1\"} 0",
            "characterization_exponential_histogram_bucket{le=\"2\"} 0",
            "characterization_exponential_histogram_bucket{le=\"4\"} 1",
            "characterization_exponential_histogram_bucket{le=\"+Inf\"} 1",
        }, await Export("characterization_exponential_histogram_bucket"));
        Assert.Equal(new[] {
            "characterization_linear_histogram_counted_bucket{le=\"0\"} 0",
            "characterization_linear_histogram_counted_bucket{le=\"5\"} 3",
            "characterization_linear_histogram_counted_bucket{le=\"+Inf\"} 3",
        }, await Export("characterization_linear_histogram_counted_bucket"));
    }

    [Fact]
    public async Task Request_metrics_label_namespace_name_and_kind()
    {
        var service = new PrometheusMetricsService();
        var command = new CharacterizationCommand();

        service.Increment(new RequestCounterMetric(command));
        service.Increment(new RequestFailureMetric(command, new TimeoutException()));

        Assert.Contains("generic_request_counter{namespace=\"ServiceComponents.Test\",name=\"CharacterizationCommand\",kind=\"command\"} 1", await Export("generic_request_counter"));
        Assert.Contains("request_failure_counter{exception_name=\"TimeoutException\",namespace=\"ServiceComponents.Test\",name=\"CharacterizationCommand\",kind=\"command\"} 1", await Export("request_failure_counter"));
    }

    [Fact]
    public async Task Request_gauge_labels_are_its_attribute_properties()
    {
        var service = new PrometheusMetricsService();

        service.Increment(new RequestGaugeMetric("characterization_gauge_name", "characterization gauge title"), 2);
        service.Decrement(new RequestGaugeMetric("characterization_gauge_name", "characterization gauge title"));

        Assert.Contains("generic_request_gauge{Name=\"characterization_gauge_name\",Title=\"characterization gauge title\",TypeId=\"ServiceComponents.Infrastructure.Monitoring.RequestGaugeMetric\"} 1", await Export("generic_request_gauge"));
    }

    [Fact]
    public void Wrong_metric_kind_throws_invalid_operation_with_the_attribute_name()
    {
        var service = new PrometheusMetricsService();

        Assert.Equal("Increment cannot be used on metric annotated as SummaryMetric", Assert.Throws<InvalidOperationException>(() => service.Increment(new SampleSummary("x"))).Message);
        Assert.Equal("Decrement cannot be used on metric annotated as CounterMetric", Assert.Throws<InvalidOperationException>(() => service.Decrement(new SampleCounter("a", "b", 1))).Message);
        Assert.Equal("Set cannot be used on metric annotated as SummaryMetric", Assert.Throws<InvalidOperationException>(() => service.Set(new SampleSummary("x"), 1)).Message);
        Assert.Equal("Observe cannot be used on metric annotated as CounterMetric", Assert.Throws<InvalidOperationException>(() => service.Observe(new SampleCounter("a", "b", 1), 1)).Message);
        Assert.Equal("Observe cannot be used on metric annotated as SummaryMetric", Assert.Throws<InvalidOperationException>(() => service.Observe(new SampleSummary("x"), 1, 1)).Message);
    }

    [Fact]
    public void Unannotated_metric_throws_null_reference_every_time()
    {
        var service = new PrometheusMetricsService();

        Assert.Throws<NullReferenceException>(() => service.Increment(new UnannotatedMetric()));
        Assert.Throws<NullReferenceException>(() => service.Increment(new UnannotatedMetric()));
        Assert.Throws<NullReferenceException>(() => service.Decrement(new UnannotatedMetric()));
        Assert.Throws<NullReferenceException>(() => service.Set(new UnannotatedMetric(), 1));
        Assert.Throws<NullReferenceException>(() => service.Observe(new UnannotatedMetric(), 1));
        Assert.Throws<NullReferenceException>(() => service.Observe(new UnannotatedMetric(), 1, 1));
    }

    [Fact]
    public void Null_label_value_throws_null_reference_every_time()
    {
        var service = new PrometheusMetricsService();

        Assert.Throws<NullReferenceException>(() => service.Increment(new NullLabelCounter(null)));
        service.Increment(new NullLabelCounter("ok"));
        Assert.Throws<NullReferenceException>(() => service.Increment(new NullLabelCounter(null)));
    }

    [Fact]
    public void Throwing_label_getter_surfaces_as_target_invocation()
    {
        var service = new PrometheusMetricsService();

        var exception = Assert.Throws<TargetInvocationException>(() => service.Increment(new ThrowingLabelCounter()));

        Assert.Equal("getter failed", exception.InnerException!.Message);
    }

    [Fact]
    public void Metric_value_extensions_read_public_instance_properties_in_declaration_order()
    {
        Assert.Equal(new[] { "first_label", "Second", "Number" }, new SampleCounter("a", "b", 3).MetricFields());
        Assert.Equal(new[] { "a", "b", "3" }, new SampleCounter("a", "b", 3).MetricValues());
        Assert.Equal(("characterization_counter", "Characterization counter"), new SampleCounter("a", "b", 3).MetricDescription());
        Assert.Equal(("UnannotatedMetric", "ServiceComponents.Test"), new UnannotatedMetric().MetricDescription());
    }
}
