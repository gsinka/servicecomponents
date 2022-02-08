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
    public class HttpQueryAuthorizationBehavior : HttpAuthorizationBehavior, IReceiveHttpQuery
    {
        private readonly IReceiveQuery _next;

        public HttpQueryAuthorizationBehavior(ILogger log, ILifetimeScope scope, IAuthorizationPolicyProvider policyProvider, IAuthorizationService authorizationService, IHttpContextAccessor httpContextAccessor, IReceiveQuery next) 
            : base(log, scope, policyProvider, authorizationService, httpContextAccessor)
        {
            _next = next;
        }

        public async Task<TResult> ReceiveAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default)
        {
            dynamic handler = Scope.ResolveOptional(typeof(IHandleQuery<,>).MakeGenericType(query.GetType(), typeof(TResult))) ?? throw new InvalidOperationException($"No handler found for {query.DisplayName()}");
            await AuthorizeRequest(handler);
            return await _next.ReceiveAsync(query, cancellationToken);
        }
    }
}