using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Serilog.Events;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Application;
using ServiceComponents.AspNet.Http;
using ServiceComponents.AspNet.Http.Senders;
using ServiceComponents.Infrastructure.CorrelationContext;
using ServiceComponents.Infrastructure.Rabbit;
using ServiceComponents.Infrastructure.Rabbit.Senders;
using ServiceComponents.Infrastructure.Receivers.Loopback;
using Xunit;
using HttpRequestOptions = ServiceComponents.AspNet.Http.HttpRequestOptions;

namespace ServiceComponents.Test;

/// <summary>
/// Pins how the transport correlation decorators fill the correlation / headers and what they log
/// (Debug level, destructured correlation). Must pass before and after the performance work.
/// </summary>
public class CorrelationLoggingCharacterizationTests
{
    public class PingCommand : ICommand
    {
        public string CommandId { get; }
        public string Text { get; }

        public PingCommand(string commandId, string text)
        {
            CommandId = commandId;
            Text = text;
        }
    }

    private static BasicDeliverEventArgs Delivery(IReadOnlyBasicProperties properties) =>
        new("ctag", 7, false, "exchange", "routing", properties, ReadOnlyMemory<byte>.Empty, CancellationToken.None);

    [Theory]
    [InlineData(LogEventLevel.Debug, true)]
    [InlineData(LogEventLevel.Verbose, true)]
    [InlineData(LogEventLevel.Information, false)]
    public async Task Loopback_receiver_copies_parent_correlation(LogEventLevel level, bool logged)
    {
        var capture = new LogCapture(level);
        var correlation = new Correlation();
        var next = new Mock<IReceiveLoopbackCommand>();
        var behavior = new LoopbackCommandReceiverCorrelationBehavior(capture.Logger, next.Object, correlation);
        var parent = new Correlation { CorrelationId = "corr-1", CausationId = "ignored", CurrentId = "parent-1" };
        var command = new PingCommand("cmd-1", "hello");

        await behavior.ReceiveAsync(command, parent);

        Assert.Equal("corr-1", correlation.CorrelationId);
        Assert.Equal("parent-1", correlation.CausationId);
        Assert.Equal("cmd-1", correlation.CurrentId);
        next.Verify(receiver => receiver.ReceiveAsync(command, parent, default), Times.Once);
        Assert.Equal(logged
            ? new[] { "Debug | Correlation in loopback scope updated from parent | correlation=Correlation { CorrelationId: \"corr-1\", CausationId: \"parent-1\", CurrentId: \"cmd-1\" }" }
            : Array.Empty<string>(), capture.Lines);
    }

    [Fact]
    public async Task Loopback_receiver_generates_correlation_id_when_parent_has_none()
    {
        var correlation = new Correlation();
        var behavior = new LoopbackCommandReceiverCorrelationBehavior(new LogCapture(LogEventLevel.Information).Logger, Mock.Of<IReceiveLoopbackCommand>(), correlation);

        await behavior.ReceiveAsync(new PingCommand("cmd-1", "hello"), new Correlation());

        Assert.True(Guid.TryParse(correlation.CorrelationId, out _));
        Assert.Null(correlation.CausationId);
    }

    [Theory]
    [InlineData(LogEventLevel.Debug, true)]
    [InlineData(LogEventLevel.Information, false)]
    public async Task Rabbit_receiver_reads_correlation_from_basic_properties(LogEventLevel level, bool logged)
    {
        var capture = new LogCapture(level);
        var correlation = new Correlation();
        var next = new Mock<IReceiveRabbitCommand>();
        var behavior = new RabbitCommandReceiverCorrelationBehavior(capture.Logger, correlation, next.Object);
        var properties = new BasicProperties {
            CorrelationId = "corr-2",
            Headers = new Dictionary<string, object> { { "causation_id", Encoding.UTF8.GetBytes("cause-2") } }
        };

        await behavior.ReceiveAsync(new PingCommand("cmd-2", "hello"), Delivery(properties), CancellationToken.None);

        Assert.Equal("corr-2", correlation.CorrelationId);
        Assert.Equal("cause-2", correlation.CausationId);
        Assert.Equal("cmd-2", correlation.CurrentId);
        Assert.Equal(logged
            ? new[] { "Debug | Correlation updated from RabbitMQ basic properties | correlation=Correlation { CorrelationId: \"corr-2\", CausationId: \"cause-2\", CurrentId: \"cmd-2\" }" }
            : Array.Empty<string>(), capture.Lines);
    }

    [Theory]
    [InlineData(LogEventLevel.Debug, true)]
    [InlineData(LogEventLevel.Information, false)]
    public async Task Rabbit_sender_writes_correlation_into_basic_properties(LogEventLevel level, bool logged)
    {
        var capture = new LogCapture(level);
        var correlation = new Correlation { CorrelationId = "corr-3", CausationId = "cause-3", CurrentId = "current-3" };
        var next = new Mock<ISendRabbitCommand>();
        var behavior = new RabbitCommandSenderCorrelationBehavior(capture.Logger, correlation, next.Object);
        var properties = new BasicProperties { Headers = new Dictionary<string, object>() };

        await behavior.SendAsync(new PingCommand("cmd-3", "hello"), properties, CancellationToken.None);

        Assert.Equal("corr-3", properties.CorrelationId);
        Assert.Equal("current-3", properties.Headers["causation_id"]);
        Assert.Equal(logged
            ? new[] { "Debug | RabbitMQ basic properties updated with correlation | correlation={ correlationId: \"corr-3\", causationId: \"current-3\" }" }
            : Array.Empty<string>(), capture.Lines);
    }

    [Theory]
    [InlineData(LogEventLevel.Debug, true)]
    [InlineData(LogEventLevel.Information, false)]
    public async Task Http_sender_writes_correlation_headers(LogEventLevel level, bool logged)
    {
        var capture = new LogCapture(level);
        var correlation = new Correlation { CorrelationId = "corr-4", CausationId = "cause-4", CurrentId = "current-4" };
        var next = new Mock<ISendHttpCommand>();
        var behavior = new HttpCommandSenderCorrelationBehavior(capture.Logger, correlation, Options.Create(new HttpRequestOptions()), next.Object);
        var headers = new Dictionary<string, string>();

        await behavior.SendAsync(new PingCommand("cmd-4", "hello"), headers);

        Assert.Equal(new Dictionary<string, string> { { "correlation-id", "corr-4" }, { "causation-id", "current-4" } }, headers);
        Assert.Equal(logged
            ? new[] { "Debug | HttpHeaders updated with correlation | correlation={ correlationId: \"corr-4\", causationId: \"cause-4\" }" }
            : Array.Empty<string>(), capture.Lines);
    }

    [Theory]
    [InlineData(LogEventLevel.Debug, true)]
    [InlineData(LogEventLevel.Information, false)]
    public async Task Http_receiver_reads_correlation_headers(LogEventLevel level, bool logged)
    {
        var capture = new LogCapture(level);
        var correlation = new Correlation();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["correlation-id"] = "corr-5";
        httpContext.Request.Headers["causation-id"] = "cause-5";
        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        var next = new Mock<IReceiveHttpCommand>();
        var behavior = new HttpCommandReceiverCorrelationBehavior(capture.Logger, accessor, Options.Create(new HttpRequestOptions()), correlation, next.Object);

        await behavior.ReceiveAsync(new PingCommand("cmd-5", "hello"));

        Assert.Equal("corr-5", correlation.CorrelationId);
        Assert.Equal("cause-5", correlation.CausationId);
        Assert.Equal("cmd-5", correlation.CurrentId);
        Assert.Equal(logged
            ? new[] { "Debug | Correlation updated from HttpContext | correlation=Correlation { CorrelationId: \"corr-5\", CausationId: \"cause-5\", CurrentId: \"cmd-5\" }" }
            : Array.Empty<string>(), capture.Lines);
    }
}
