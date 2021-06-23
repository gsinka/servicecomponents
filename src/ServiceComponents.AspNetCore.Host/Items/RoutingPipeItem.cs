using Microsoft.AspNetCore.Builder;
using ServiceComponents.AspNetCore.Hosting.Enumerables;

namespace ServiceComponents.AspNetCore.Hosting.Items
{
    public class RoutingPipeItem : PipeItem
    {
        public RoutingPipeItem() 
            : base(RequestPipeEnumerations.Routing.Name, (context, builder) => builder.UseRouting())
        {
        }
    }
}
