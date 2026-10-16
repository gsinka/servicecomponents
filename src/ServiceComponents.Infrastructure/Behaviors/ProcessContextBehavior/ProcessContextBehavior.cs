using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ServiceComponents.Api.Contexts;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Application.Mediator;

namespace ServiceComponents.Infrastructure.Behaviors.ProcessContextBehavior
{
    public class ProcessContextBehavior : IHandleCommandFailure, IHandleQueryFailure
    {
        private readonly ProcessContextBehaviorOptions _options;

        public ProcessContextBehavior(ProcessContextBehaviorOptions options)
        {
            _options = options;
        }

        public Task HandleFailureAsync(ICommand command, Exception exception, CancellationToken cancellationToken = default)
        {
            SetContext(exception, command.GetType().GetCustomAttribute<ProcessContextAttribute>());

            return Task.CompletedTask;
        }

        public Task HandleFailureAsync(IQuery query, Exception exception, CancellationToken cancellationToken = default)
        {
            SetContext(exception, query.GetType().GetCustomAttribute<ProcessContextAttribute>());

            return Task.CompletedTask;
        }

        private void SetContext(Exception exception, ProcessContextAttribute? attribute)
        {
            if (attribute is null) {
                return;
            }

            // The same exception instance can pass through several dispatch levels (nested commands/queries);
            // the innermost context, where the failure originated, wins.
            if (exception.Data.Contains(_options.titleKey)) {
                return;
            }

            exception.Data[_options.titleKey] = attribute.Title;
            if (!string.IsNullOrWhiteSpace(attribute.ContextCode)) {
                exception.Data[_options.contextCodeKey] = attribute.ContextCode;
            }
        }
    }
}
