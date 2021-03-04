using System;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using Serilog.Context;
using Serilog.Events;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Application.Mediator;
using ServiceComponents.Core.ExtensionMethods;

namespace ServiceComponents.Infrastructure.Behaviors.Logging
{
    public class LogBehavior :
        IPreHandleCommand, IPostHandleCommand, IHandleCommandFailure,
        IPreHandleQuery, IPostHandleQuery, IHandleQueryFailure,
        IPreHandleEvent, IPostHandleEvent, IHandleEventFailure
    {
        private readonly ILogger _log;
        private readonly LogEventLevel _level;

        public LogBehavior(ILogger log, LogEventLevel level)
        {
            _log = log;
            _level = level;
        }

        public Task PreHandleAsync(ICommand command, CancellationToken cancellationToken = default)
        {
            _log.ForContext("commandData", command, true).Verbose("Handling {commandType}", command.DisplayName());
            return Task.CompletedTask;
        }

        public Task PostHandleAsync(ICommand command, CancellationToken cancellationToken = default)
        {
            _log.Verbose("Handling {commandType} succeeded", command.DisplayName());
            return Task.CompletedTask;
        }

        public Task HandleFailureAsync(ICommand command, Exception exception, CancellationToken cancellationToken = default)
        {
            _log.Error("Handling {commandType} failed: {error}", command.DisplayName(), exception.Message);
            return Task.CompletedTask;
        }

        public Task PreHandleAsync(IQuery query, CancellationToken cancellationToken = default)
        {
            _log.ForContext("queryData", query, true).Verbose("Handling {queryType}", query.DisplayName());
            return Task.CompletedTask;
        }

        public Task PostHandleAsync(IQuery query, object result, CancellationToken cancellationToken = default)
        {
            _log.ForContext("queryResult", result, true).Verbose("Handling {queryType} succeeded", query.DisplayName());
            return Task.CompletedTask;
        }

        public Task HandleFailureAsync(IQuery query, Exception exception, CancellationToken cancellationToken = default)
        {
            _log.Error("Handling {queryType} failed: {error}", query.DisplayName(), exception.Message);
            return Task.CompletedTask;
        }

        public Task PreHandleAsync(IEvent @event, CancellationToken cancellationToken = default)
        {
            _log.ForContext("eventData", @event, true).Debug("Handling {eventType}", @event.DisplayName());
            return Task.CompletedTask;
        }

        public Task PostHandleAsync(IEvent @event, CancellationToken cancellationToken = default)
        {
            _log.Verbose("Handling {eventType} succeeded", @event.DisplayName());
            return Task.CompletedTask;
        }

        public Task HandleFailureAsync(IEvent @event, Exception exception, CancellationToken cancellationToken = default)
        {
            _log.Error("Handling {eventType} failed: {error}", @event.DisplayName(), exception.Message);
            return Task.CompletedTask;
        }
    }
}