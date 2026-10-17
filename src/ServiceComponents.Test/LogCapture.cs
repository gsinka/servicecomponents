using System;
using System.Collections.Generic;
using System.Linq;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace ServiceComponents.Test;

/// <summary>
/// Serilog logger that keeps every emitted event and renders each one into a single comparable line:
/// level | message template | properties (sorted, including destructured objects) | exception type.
/// </summary>
public sealed class LogCapture : ILogEventSink
{
    private readonly List<LogEvent> _events = new();

    public LogCapture(LogEventLevel minimumLevel)
    {
        Logger = new LoggerConfiguration()
            .MinimumLevel.Is(minimumLevel)
            .WriteTo.Sink(this)
            .CreateLogger();
    }

    public ILogger Logger { get; }

    public IReadOnlyList<string> Lines
    {
        get {
            lock (_events) {
                return _events.Select(Describe).ToArray();
            }
        }
    }

    public void Emit(LogEvent logEvent)
    {
        lock (_events) {
            _events.Add(logEvent);
        }
    }

    private static string Describe(LogEvent logEvent)
    {
        var properties = string.Join("; ", logEvent.Properties
            .OrderBy(property => property.Key, StringComparer.Ordinal)
            .Select(property => $"{property.Key}={property.Value}"));

        var exception = logEvent.Exception == null ? "" : $" | {logEvent.Exception.GetType().Name}: {logEvent.Exception.Message}";

        return $"{logEvent.Level} | {logEvent.MessageTemplate.Text} | {properties}{exception}";
    }
}
