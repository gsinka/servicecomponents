using System;
using Microsoft.AspNetCore.Builder;
using ServiceComponents.AspNetCore.Hosting.Enumerables;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace ServiceComponents.AspNetCore.Hosting.Items
{
    public class SwaggerPipeItem : PipeItem
    {
        public SwaggerPipeItem(Action<SwaggerUIOptions> configure) 
            : base(RequestPipeEnumerations.Swagger.Name, (context, builder) => {
                builder.UseSwagger();
                builder.UseSwaggerUI(configure);
            })
        {
        }
    }
}
