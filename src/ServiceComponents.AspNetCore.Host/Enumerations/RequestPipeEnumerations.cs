using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using ServiceComponents.Core;

namespace ServiceComponents.AspNetCore.Hosting.Enumerables
{
    public class RequestPipeEnumerations : Enumeration
    {
        public static RequestPipeEnumerations Routing => new RequestPipeEnumerations(0, "Routing");
        public static RequestPipeEnumerations Swagger => new RequestPipeEnumerations(1, "Swagger");
        public static RequestPipeEnumerations Endpoints => new RequestPipeEnumerations(2, "Endpoints");

        protected RequestPipeEnumerations(int id, string name)
            : base(id, name)
        {

        }
    }
}
