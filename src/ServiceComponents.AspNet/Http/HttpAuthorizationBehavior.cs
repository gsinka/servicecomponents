using System;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Serilog;

namespace ServiceComponents.AspNet.Http
{
    public abstract class HttpAuthorizationBehavior
    {
        protected readonly ILifetimeScope Scope;
        private readonly IAuthorizationPolicyProvider _policyProvider;
        private readonly IAuthorizationService _authorizationService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        protected HttpAuthorizationBehavior(ILogger log, ILifetimeScope scope, IAuthorizationPolicyProvider policyProvider, IAuthorizationService authorizationService, IHttpContextAccessor httpContextAccessor)
        {
            Scope = scope;
            _policyProvider = policyProvider;
            _authorizationService = authorizationService;
            _httpContextAccessor = httpContextAccessor;
        }

        protected async Task AuthorizeRequest(object handler)
        {
            var authorizeData = (IAuthorizeData[])Attribute.GetCustomAttributes(handler.GetType(), typeof(AuthorizeAttribute));
            var policy = await AuthorizationPolicy.CombineAsync(_policyProvider, authorizeData);

            if (policy != null)
            {
                var authorizationResult = await _authorizationService.AuthorizeAsync(_httpContextAccessor?.HttpContext?.User!, null, policy);
                if (!authorizationResult.Succeeded) throw new UnauthorizedAccessException(string.Join("\r\n", authorizationResult.Failure.FailedRequirements.Select(requirement => requirement.ToString())));
            }
        }
    }
}