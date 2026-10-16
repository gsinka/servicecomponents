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

            exception.Data.Add(_options.titleKey, attribute.Title);
            if (!string.IsNullOrWhiteSpace(attribute.ContextCode)) {
                exception.Data.Add(_options.contextCodeKey, attribute.ContextCode);
            }
        }
    }
}
