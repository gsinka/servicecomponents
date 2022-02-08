using System;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Serilog;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Application.Mediator;
using ServiceComponents.Core.Extensions;
using ServiceComponents.Infrastructure.Receivers;

namespace ServiceComponents.AspNet.Http
{
    public class HttpCommandAuthorizationBehavior : HttpAuthorizationBehavior, IReceiveHttpCommand
    {
        private readonly IReceiveCommand _next;

        public HttpCommandAuthorizationBehavior(ILogger log, ILifetimeScope scope, IAuthorizationPolicyProvider policyProvider, IAuthorizationService authorizationService, IHttpContextAccessor httpContextAccessor, IReceiveCommand next) 
            : base(log, scope, policyProvider, authorizationService, httpContextAccessor)
        {
            _next = next;
        }

        public async Task ReceiveAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default) where TCommand : ICommand
        {
            dynamic handler = Scope.ResolveOptional(typeof(IHandleCommand<>).MakeGenericType(command.GetType())) ?? throw new InvalidOperationException($"No handler registered for {command.DisplayName()}");
            await AuthorizeRequest(handler);
            await _next.ReceiveAsync(command, cancellationToken);
        }
    }
}
