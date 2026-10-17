using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using FluentValidation;
using Serilog.Events;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Application.Mediator;
using ServiceComponents.Infrastructure.Behaviors.Stopwatch;
using ServiceComponents.Infrastructure.Dispatchers;
using ServiceComponents.Infrastructure.Mediator;
using ServiceComponents.Infrastructure.Validation;
using Xunit;

namespace ServiceComponents.Test;

/// <summary>
/// Pins the dispatch pipeline: which hooks run, in which order, how failures and validation surface,
/// and what happens without a handler. Must pass before and after the performance work.
/// </summary>
public class MediatorPipelineCharacterizationTests
{
    // Messages (only used by this test class, so assembly scanning of this test assembly stays predictable)

    public class PipelineCommand : ICommand
    {
        public string CommandId { get; }
        public string Name { get; }
        public bool Fail { get; }

        public PipelineCommand(string commandId, string name, bool fail = false)
        {
            CommandId = commandId;
            Name = name;
            Fail = fail;
        }
    }

    public class PipelineCommandWithoutHandler : ICommand
    {
        public string CommandId => "no-handler";
    }

    public class PipelineQuery : IQuery<int>
    {
        public string QueryId { get; }
        public int Value { get; }

        public PipelineQuery(string queryId, int value)
        {
            QueryId = queryId;
            Value = value;
        }
    }

    public class PipelineQueryWithoutHandler : IQuery<int>
    {
        public string QueryId => "no-handler";
    }

    public class PipelineEvent : IEvent
    {
        public string EventId { get; }
        public bool Fail { get; }

        public PipelineEvent(string eventId, bool fail = false)
        {
            EventId = eventId;
            Fail = fail;
        }
    }

    public class PipelineEventWithoutHandler : IEvent
    {
        public string EventId => "no-handler";
    }

    public class Recorder
    {
        private readonly List<string> _steps = new();

        public void Add(string step)
        {
            lock (_steps) {
                _steps.Add(step);
            }
        }

        public string[] Steps
        {
            get {
                lock (_steps) {
                    return _steps.ToArray();
                }
            }
        }
    }

    // Handlers

    public class PipelineCommandHandler : IHandleCommand<PipelineCommand>
    {
        private readonly Recorder _recorder;
        public PipelineCommandHandler(Recorder recorder) => _recorder = recorder;

        public Task HandleAsync(PipelineCommand command, CancellationToken cancellationToken = default)
        {
            _recorder.Add($"handler:{command.Name}");
            return command.Fail ? throw new InvalidOperationException("command handler failed") : Task.CompletedTask;
        }
    }

    public class PipelineQueryHandler : IHandleQuery<PipelineQuery, int>
    {
        private readonly Recorder _recorder;
        public PipelineQueryHandler(Recorder recorder) => _recorder = recorder;

        public Task<int> HandleAsync(PipelineQuery query, CancellationToken cancellationToken = default)
        {
            _recorder.Add($"handler:{query.Value}");
            return query.Value < 0 ? throw new ArgumentOutOfRangeException(nameof(query), "negative") : Task.FromResult(query.Value * 2);
        }
    }

    public class FirstPipelineEventHandler : IHandleEvent<PipelineEvent>
    {
        private readonly Recorder _recorder;
        public FirstPipelineEventHandler(Recorder recorder) => _recorder = recorder;

        public Task HandleAsync(PipelineEvent @event, CancellationToken cancellationToken = default)
        {
            _recorder.Add("handler:first");
            return Task.CompletedTask;
        }
    }

    public class SecondPipelineEventHandler : IHandleEvent<PipelineEvent>
    {
        private readonly Recorder _recorder;
        public SecondPipelineEventHandler(Recorder recorder) => _recorder = recorder;

        public Task HandleAsync(PipelineEvent @event, CancellationToken cancellationToken = default)
        {
            _recorder.Add("handler:second");
            return @event.Fail ? throw new InvalidOperationException("event handler failed") : Task.CompletedTask;
        }
    }

    // Hooks

    public class PipelineCommandValidator : AbstractValidator<PipelineCommand>
    {
        public PipelineCommandValidator() => RuleFor(command => command.Name).NotEmpty();
    }

    public class PipelineQueryValidator : AbstractValidator<PipelineQuery>
    {
        public PipelineQueryValidator() => RuleFor(query => query.Value).NotEqual(13);
    }

    public class GenericCommandHooks : IPreHandleCommand<PipelineCommand>, IPostHandleCommand<PipelineCommand>, IHandleCommandFailure<PipelineCommand>
    {
        private readonly Recorder _recorder;
        public GenericCommandHooks(Recorder recorder) => _recorder = recorder;

        public Task PreHandleAsync(PipelineCommand command, CancellationToken cancellationToken = default) { _recorder.Add("pre<T>"); return Task.CompletedTask; }
        public Task PostHandleAsync(PipelineCommand command, CancellationToken cancellationToken = default) { _recorder.Add("post<T>"); return Task.CompletedTask; }
        public Task HandleFailureAsync(PipelineCommand command, Exception exception, CancellationToken cancellationToken = default) { _recorder.Add($"failure<T>:{exception.GetType().Name}"); return Task.CompletedTask; }
    }

    public class GenericQueryHooks : IPreHandleQuery<PipelineQuery, int>, IPostHandleQuery<PipelineQuery, int>, IHandleQueryFailure<PipelineQuery, int>
    {
        private readonly Recorder _recorder;
        public GenericQueryHooks(Recorder recorder) => _recorder = recorder;

        public Task PreHandleAsync(PipelineQuery query, CancellationToken cancellationToken = default) { _recorder.Add("pre<T>"); return Task.CompletedTask; }
        public Task<int> PostHandleAsync(PipelineQuery query, int result, CancellationToken cancellationToken = default) { _recorder.Add($"post<T>:{result}"); return Task.FromResult(-1); }
        public Task HandleFailureAsync(PipelineQuery query, Exception exception, CancellationToken cancellationToken = default) { _recorder.Add($"failure<T>:{exception.GetType().Name}"); return Task.CompletedTask; }
    }

    public class GenericEventHooks : IPreHandleEvent<PipelineEvent>, IPostHandleEvent<PipelineEvent>, IHandleEventFailure<PipelineEvent>
    {
        private readonly Recorder _recorder;
        public GenericEventHooks(Recorder recorder) => _recorder = recorder;

        public Task PreHandleAsync(PipelineEvent @event, CancellationToken cancellationToken = default) { _recorder.Add("pre<T>"); return Task.CompletedTask; }
        public Task PostHandleAsync(PipelineEvent @event, CancellationToken cancellationToken = default) { _recorder.Add("post<T>"); return Task.CompletedTask; }
        public Task HandleFailureAsync(PipelineEvent @event, Exception exception, CancellationToken cancellationToken = default) { _recorder.Add($"failure<T>:{exception.GetType().Name}"); return Task.CompletedTask; }
    }

    public class NonGenericHooks :
        IPreHandleCommand, IPostHandleCommand, IHandleCommandFailure,
        IPreHandleQuery, IPostHandleQuery, IHandleQueryFailure,
        IPreHandleEvent, IPostHandleEvent, IHandleEventFailure
    {
        private readonly Recorder _recorder;
        public NonGenericHooks(Recorder recorder) => _recorder = recorder;

        public Task PreHandleAsync(ICommand command, CancellationToken cancellationToken = default) { _recorder.Add("pre"); return Task.CompletedTask; }
        public Task PostHandleAsync(ICommand command, CancellationToken cancellationToken = default) { _recorder.Add("post"); return Task.CompletedTask; }
        public Task HandleFailureAsync(ICommand command, Exception exception, CancellationToken cancellationToken = default) { _recorder.Add($"failure:{exception.GetType().Name}"); return Task.CompletedTask; }
        public Task PreHandleAsync(IQuery query, CancellationToken cancellationToken = default) { _recorder.Add("pre"); return Task.CompletedTask; }
        public Task PostHandleAsync(IQuery query, object result, CancellationToken cancellationToken = default) { _recorder.Add($"post:{result}"); return Task.CompletedTask; }
        public Task HandleFailureAsync(IQuery query, Exception exception, CancellationToken cancellationToken = default) { _recorder.Add($"failure:{exception.GetType().Name}"); return Task.CompletedTask; }
        public Task PreHandleAsync(IEvent @event, CancellationToken cancellationToken = default) { _recorder.Add("pre"); return Task.CompletedTask; }
        public Task PostHandleAsync(IEvent @event, CancellationToken cancellationToken = default) { _recorder.Add("post"); return Task.CompletedTask; }
        public Task HandleFailureAsync(IEvent @event, Exception exception, CancellationToken cancellationToken = default) { _recorder.Add($"failure:{exception.GetType().Name}"); return Task.CompletedTask; }
    }

    // Setup

    private static (IContainer container, Recorder recorder, LogCapture log) Build(bool behaviors = true, bool stopwatch = false)
    {
        var assembly = typeof(MediatorPipelineCharacterizationTests).Assembly;
        var recorder = new Recorder();
        var log = new LogCapture(LogEventLevel.Verbose);
        var builder = new ContainerBuilder();
        builder.RegisterInstance(recorder);
        builder.RegisterInstance(log.Logger);
        builder.AddMediator(assembly);
        if (behaviors) {
            builder.AddMediatorBehavior(assembly);
            builder.AddValidationBehavior(assembly);
        }
        if (stopwatch) {
            builder.AddStopwatchBehavior();
        }

        return (builder.Build(), recorder, log);
    }

    [Fact]
    public async Task Command_runs_hooks_in_order()
    {
        var (container, recorder, log) = Build();
        await using var scope = container.BeginLifetimeScope();

        await scope.Resolve<IDispatchCommand>().DispatchAsync(new PipelineCommand("c-1", "ok"));

        Assert.Equal(new[] { "pre", "pre<T>", "handler:ok", "post<T>", "post" }, recorder.Steps);
        Assert.Contains("Verbose | Dispatching {commandType} to {handlerType} | commandType=\"PipelineCommand\"; handlerType=\"PipelineCommandHandler\"", log.Lines);
    }

    [Fact]
    public async Task Command_failure_runs_failure_hooks_and_rethrows_the_same_exception()
    {
        var (container, recorder, _) = Build();
        await using var scope = container.BeginLifetimeScope();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => scope.Resolve<IDispatchCommand>().DispatchAsync(new PipelineCommand("c-1", "bad", fail: true)));

        Assert.Equal("command handler failed", exception.Message);
        Assert.Equal(new[] { "pre", "pre<T>", "handler:bad", "failure:InvalidOperationException", "failure<T>:InvalidOperationException" }, recorder.Steps);
    }

    [Fact]
    public async Task Command_validation_failure_stops_before_the_handler()
    {
        var (container, recorder, _) = Build();
        await using var scope = container.BeginLifetimeScope();

        var exception = await Assert.ThrowsAsync<ValidationException>(() => scope.Resolve<IDispatchCommand>().DispatchAsync(new PipelineCommand("c-1", "")));

        Assert.Equal("Name", Assert.Single(exception.Errors).PropertyName);
        Assert.DoesNotContain(recorder.Steps, step => step.StartsWith("handler"));
        Assert.Equal("failure:ValidationException", recorder.Steps[^2]);
        Assert.Equal("failure<T>:ValidationException", recorder.Steps[^1]);
    }

    [Fact]
    public async Task Command_without_handler_throws_invalid_operation()
    {
        var (container, recorder, _) = Build();
        await using var scope = container.BeginLifetimeScope();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => scope.Resolve<IDispatchCommand>().DispatchAsync(new PipelineCommandWithoutHandler()));

        Assert.Equal("No handler registered for PipelineCommandWithoutHandler", exception.Message);
        Assert.Equal(new[] { "pre", "failure:InvalidOperationException" }, recorder.Steps);
    }

    [Fact]
    public async Task Command_dispatched_through_its_interface_type_uses_the_runtime_type()
    {
        var (container, recorder, _) = Build();
        await using var scope = container.BeginLifetimeScope();
        ICommand command = new PipelineCommand("c-1", "as-interface");

        await scope.Resolve<IDispatchCommand>().DispatchAsync(command);

        Assert.Equal(new[] { "pre", "pre<T>", "handler:as-interface", "post<T>", "post" }, recorder.Steps);
    }

    [Fact]
    public async Task Query_runs_hooks_in_order_and_returns_the_handler_result()
    {
        var (container, recorder, log) = Build();
        await using var scope = container.BeginLifetimeScope();

        var result = await scope.Resolve<IDispatchQuery>().DispatchAsync(new PipelineQuery("q-1", 21));

        Assert.Equal(42, result);
        Assert.Equal(new[] { "pre", "pre<T>", "handler:21", "post:42", "post<T>:42" }, recorder.Steps);
        Assert.Contains("Verbose | Dispatching {eventType} to {handlerType} | eventType=\"PipelineQuery\"; handlerType=\"PipelineQueryHandler\"", log.Lines);
    }

    [Fact]
    public async Task Query_failure_runs_failure_hooks_and_rethrows()
    {
        var (container, recorder, _) = Build();
        await using var scope = container.BeginLifetimeScope();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => scope.Resolve<IDispatchQuery>().DispatchAsync(new PipelineQuery("q-1", -1)));

        Assert.Equal(new[] { "pre", "pre<T>", "handler:-1", "failure:ArgumentOutOfRangeException", "failure<T>:ArgumentOutOfRangeException" }, recorder.Steps);
    }

    [Fact]
    public async Task Query_validation_failure_stops_before_the_handler()
    {
        var (container, recorder, _) = Build();
        await using var scope = container.BeginLifetimeScope();

        await Assert.ThrowsAsync<ValidationException>(() => scope.Resolve<IDispatchQuery>().DispatchAsync(new PipelineQuery("q-1", 13)));

        Assert.DoesNotContain(recorder.Steps, step => step.StartsWith("handler"));
    }

    [Fact]
    public async Task Query_without_handler_throws_invalid_operation()
    {
        var (container, _, _) = Build();
        await using var scope = container.BeginLifetimeScope();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => scope.Resolve<IDispatchQuery>().DispatchAsync(new PipelineQueryWithoutHandler()));

        Assert.Equal("No handler found for PipelineQueryWithoutHandler", exception.Message);
    }

    [Fact]
    public async Task Event_runs_every_handler_between_hooks()
    {
        var (container, recorder, log) = Build();
        await using var scope = container.BeginLifetimeScope();

        await scope.Resolve<IDispatchEvent>().DispatchAsync(new PipelineEvent("e-1"));

        var steps = recorder.Steps;
        Assert.Equal(new[] { "pre", "pre<T>" }, steps[..2]);
        Assert.Equal(new[] { "handler:first", "handler:second" }, steps[2..4].OrderBy(step => step));
        Assert.Equal(new[] { "post", "post<T>" }, steps[4..]);
        Assert.Contains("Verbose | Dispatching {eventType} to {handlerType} | eventType=\"PipelineEvent\"; handlerType=\"FirstPipelineEventHandler\"", log.Lines);
        Assert.Contains("Verbose | Dispatching {eventType} to {handlerType} | eventType=\"PipelineEvent\"; handlerType=\"SecondPipelineEventHandler\"", log.Lines);
    }

    [Fact]
    public async Task Event_failure_runs_failure_hooks_and_rethrows()
    {
        var (container, recorder, _) = Build();
        await using var scope = container.BeginLifetimeScope();

        await Assert.ThrowsAsync<InvalidOperationException>(() => scope.Resolve<IDispatchEvent>().DispatchAsync(new PipelineEvent("e-1", fail: true)));

        Assert.Equal(new[] { "failure:InvalidOperationException", "failure<T>:InvalidOperationException" }, recorder.Steps[^2..]);
    }

    [Fact]
    public async Task Event_without_handlers_still_runs_hooks()
    {
        var (container, recorder, _) = Build();
        await using var scope = container.BeginLifetimeScope();

        await scope.Resolve<IDispatchEvent>().DispatchAsync(new PipelineEventWithoutHandler());

        Assert.Equal(new[] { "pre", "post" }, recorder.Steps);
    }

    [Fact]
    public async Task Dispatchers_without_behaviors_call_handlers_directly()
    {
        var (container, recorder, _) = Build(behaviors: false);
        await using var scope = container.BeginLifetimeScope();

        await scope.Resolve<IDispatchCommand>().DispatchAsync(new PipelineCommand("c-1", ""));
        var result = await scope.Resolve<IDispatchQuery>().DispatchAsync(new PipelineQuery("q-1", 13));
        await scope.Resolve<IDispatchEvent>().DispatchAsync(new PipelineEvent("e-1"));

        Assert.Equal(26, result);
        Assert.Equal(new[] { "handler:", "handler:13" }, recorder.Steps[..2]);
        Assert.Equal(4, recorder.Steps.Length);
    }

    [Fact]
    public async Task Stopwatch_logs_processed_and_failed()
    {
        var (container, _, log) = Build(stopwatch: true);
        await using var scope = container.BeginLifetimeScope();

        await scope.Resolve<IDispatchCommand>().DispatchAsync(new PipelineCommand("c-1", "ok"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => scope.Resolve<IDispatchCommand>().DispatchAsync(new PipelineCommand("c-2", "bad", fail: true)));

        Assert.Contains(log.Lines, line => line.StartsWith("Verbose | {command} processed in {duration} ms | command=\"PipelineCommand\"; duration="));
        Assert.Contains(log.Lines, line => line.StartsWith("Verbose | {command} failed in {duration} ms | command=\"PipelineCommand\"; duration="));
    }

    [Fact]
    public async Task Repeated_dispatch_of_different_message_types_keeps_working()
    {
        var (container, recorder, _) = Build();

        for (var i = 0; i < 3; i++) {
            await using var scope = container.BeginLifetimeScope();
            await scope.Resolve<IDispatchCommand>().DispatchAsync(new PipelineCommand($"c-{i}", $"n{i}"));
            Assert.Equal(i * 2, await scope.Resolve<IDispatchQuery>().DispatchAsync(new PipelineQuery($"q-{i}", i)));
            await scope.Resolve<IDispatchEvent>().DispatchAsync(new PipelineEvent($"e-{i}"));
        }

        Assert.Equal(3, recorder.Steps.Count(step => step.StartsWith("handler:n")));
        Assert.Equal(3, recorder.Steps.Count(step => step == "handler:first"));
    }
}
