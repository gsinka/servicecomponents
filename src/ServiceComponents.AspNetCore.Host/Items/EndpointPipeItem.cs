using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using ServiceComponents.AspNetCore.Hosting.Enumerables;

namespace ServiceComponents.AspNetCore.Hosting.Items
{
    public class EndpointPipeItem : PipeItem
    {
        public EndpointPipeItem(Action<IEndpointRouteBuilder> configureEndpoint) 
            : base(RequestPipeEnumerations.Endpoints.Name, (context, builder) => builder.UseEndpoints(configureEndpoint))
        {
        }
    }
}
