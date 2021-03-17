using System;
using System.Collections.Generic;
using Autofac;
using Microsoft.Extensions.Configuration;

namespace ServiceComponents.AspNet.Wireup
{
    public abstract class WireupBase
    {
        protected readonly List<Action<ContainerBuilder, IConfiguration>> ContainerBuilderCallbacks = new List<Action<ContainerBuilder, IConfiguration>>();

        protected WireupBase RegisterCallback(Action<ContainerBuilder, IConfiguration> callback)
        {
            ContainerBuilderCallbacks.Add(callback);
            return this;
        }
    }
}
