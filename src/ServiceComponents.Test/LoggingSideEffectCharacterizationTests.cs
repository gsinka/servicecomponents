using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Moq;
using RabbitMQ.Client;
using Serilog;
using Serilog.Events;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Application.Mediator;
using ServiceComponents.Application.Senders;
using ServiceComponents.Infrastructure.Behaviors.Logging;
using ServiceComponents.Infrastructure.CorrelationContext;
using ServiceComponents.Infrastructure.Mediator;
using ServiceComponents.Infrastructure.Rabbit.Senders;
using ServiceComponents.Infrastructure.Receivers;
using ServiceComponents.Infrastructure.Receivers.Loopback;
using Xunit;
using ILogger = Serilog.ILogger;

namespace ServiceComponents.Test;

/// <summary>
/// Pins a side effect that applications can depend on: LogBehavior and the RabbitMQ senders walk the whole message
/// for the log (Serilog destructuring) even when that level is switched off. Getters run, collections are
/// enumerated, an exception during that walk fails the call, and lazily loaded members get loaded while their
/// scope is still alive. Skipping that walk is a faster but different behavior, so it is not part of the
/// behavior-preserving performance work.
/// </summary>
public class LoggingSideEffectCharacterizationTests
{
    public class CountingCommand : ICommand
    {
        private int _reads;

        public string CommandId => "counting";
        public int Reads => _reads;

        public string Payload
        {
            get {
                Interlocked.Increment(ref _reads);
                return "payload";
            }
        }
    }

    public class CountingQuery : IQuery<CountingResult>
    {
        public string QueryId => "counting";
    }

    public class CountingResult
    {
        private int _reads;
        public int Reads => _reads;

        public string Payload
        {
            get {
                Interlocked.Increment(ref _reads);
                return "payload";
            }
        }
    }

    public class CountingEnumerable : IEnumerable<int>
    {
        public int Enumerations;

        public IEnumerator<int> GetEnumerator()
        {
            Interlocked.Increment(ref Enumerations);
            yield return 1;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public class FailingEnumerable : IEnumerable<int>
    {
        public IEnumerator<int> GetEnumerator()
        {
            yield return 1;
            throw new InvalidOperationException("enumeration failed");
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public class CommandWithFailingCollection : ICommand
    {
        public string CommandId => "failing";
        public FailingEnumerable Items { get; } = new();
    }

    [Fact]
    public async Task Log_behavior_reads_the_message_and_the_result_even_when_verbose_is_off()
    {
        var behavior = new LogBehavior(new LogCapture(LogEventLevel.Information).Logger);
        var command = new CountingCommand();
        var result = new CountingResult();

        await behavior.PreHandleAsync(command);
        await behavior.PostHandleAsync(new CountingQuery(), result);

        Assert.Equal(1, command.Reads);
        Assert.Equal(1, result.Reads);
    }

    [Fact]
    public async Task Log_behavior_enumerates_query_results_even_when_verbose_is_off()
    {
        var behavior = new LogBehavior(new LogCapture(LogEventLevel.Information).Logger);
        var result = new CountingEnumerable();

        await behavior.PostHandleAsync(new CountingQuery(), result);

        Assert.Equal(1, result.Enumerations);
    }

    [Fact]
    public async Task An_exception_while_walking_the_message_fails_the_call_even_when_verbose_is_off()
    {
        var behavior = new LogBehavior(new LogCapture(LogEventLevel.Information).Logger);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => behavior.PreHandleAsync(new CommandWithFailingCollection()));

        Assert.Equal("enumeration failed", exception.Message);
    }

    [Fact]
    public async Task Rabbit_command_sender_reads_the_message_for_the_log_and_for_the_body()
    {
        var channel = new Mock<IChannel>();
        var sender = new RabbitCommandSender(new LogCapture(LogEventLevel.Information).Logger, channel.Object, "exchange", "key");
        var command = new CountingCommand();

        await sender.SendAsync(command, new BasicProperties { Headers = new Dictionary<string, object>() }, CancellationToken.None);

        Assert.Equal(2, command.Reads);
    }

    // A logger that implements only Write (all other members use Serilog's default implementations, which report
    // every level as enabled). The correlation decorators must still deliver their Debug entry to it.
    private sealed class WriteOnlyLogger : ILogger
    {
        public readonly List<LogEvent> Events = new();
        public void Write(LogEvent logEvent) => Events.Add(logEvent);
    }

    [Fact]
    public async Task Correlation_entries_still_reach_a_logger_that_only_implements_write()
    {
        var logger = new WriteOnlyLogger();
        var behavior = new LoopbackCommandReceiverCorrelationBehavior(logger, Mock.Of<IReceiveLoopbackCommand>(), new Correlation());

        await behavior.ReceiveAsync(new CountingCommand(), new Correlation { CorrelationId = "corr", CurrentId = "parent" });

        var logEvent = Assert.Single(logger.Events);
        Assert.Equal(LogEventLevel.Debug, logEvent.Level);
        Assert.Equal("Correlation in loopback scope updated from parent", logEvent.MessageTemplate.Text);
        Assert.Equal("Correlation { CorrelationId: \"corr\", CausationId: \"parent\", CurrentId: \"counting\" }", logEvent.Properties["correlation"].ToString());
    }

    // --- Lazy loading through a loopback query (the NHibernate case) ---
    //
    // ISession is registered per lifetime scope, and the loopback sender disposes its child scope before it returns
    // the result. Lazily loaded members of the result therefore work afterwards only if something loaded them inside
    // the scope; today the log walk does that.

    public sealed class LazySession : IDisposable
    {
        public bool Disposed { get; private set; }
        public void Dispose() => Disposed = true;
    }

    public class LazyOrderQuery : IQuery<LazyOrder>
    {
        public string QueryId => "lazy-order";
    }

    public class LazyOrder
    {
        private readonly LazySession _session;
        private List<string> _lines;

        public LazyOrder(LazySession session) => _session = session;

        public string Number => "order-1";
        public bool SessionClosed => _session.Disposed;

        public IReadOnlyList<string> Lines
        {
            get {
                if (_lines == null) {
                    if (_session.Disposed) {
                        throw new InvalidOperationException("Lazy load after the session was closed");
                    }
                    _lines = new List<string> { "apple", "pear" };
                }
                return _lines;
            }
        }
    }

    public class LazyOrderQueryHandler : IHandleQuery<LazyOrderQuery, LazyOrder>
    {
        private readonly LazySession _session;
        public LazyOrderQueryHandler(LazySession session) => _session = session;

        public Task<LazyOrder> HandleAsync(LazyOrderQuery query, CancellationToken cancellationToken = default) => Task.FromResult(new LazyOrder(_session));
    }

    private static IContainer BuildLoopback(bool logBehavior)
    {
        var builder = new ContainerBuilder();
        builder.RegisterInstance(new LogCapture(LogEventLevel.Information).Logger);
        builder.RegisterType<LazySession>().InstancePerLifetimeScope();
        builder.AddMediator(typeof(LoggingSideEffectCharacterizationTests).Assembly);
        builder.AddMediatorBehavior();
        if (logBehavior) {
            builder.AddLogBehavior();
        }
        builder.AddReceivers();
        builder.AddLoopbackReceivers();
        builder.AddLoopbackQuerySender();
        return builder.Build();
    }

    [Fact]
    public async Task Loopback_query_result_lazy_members_are_loaded_inside_the_scope_by_the_log_behavior()
    {
        await using var container = BuildLoopback(logBehavior: true);
        await using var scope = container.BeginLifetimeScope();

        var order = await scope.Resolve<ISendQuery>().SendAsync(new LazyOrderQuery());

        Assert.True(order.SessionClosed);
        Assert.Equal(new[] { "apple", "pear" }, order.Lines);
    }

    [Fact]
    public async Task Without_the_log_behavior_the_same_lazy_member_fails_after_the_scope()
    {
        await using var container = BuildLoopback(logBehavior: false);
        await using var scope = container.BeginLifetimeScope();

        var order = await scope.Resolve<ISendQuery>().SendAsync(new LazyOrderQuery());

        Assert.True(order.SessionClosed);
        Assert.Equal("Lazy load after the session was closed", Assert.Throws<InvalidOperationException>(() => order.Lines).Message);
    }
}
