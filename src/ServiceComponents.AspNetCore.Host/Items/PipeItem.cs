using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace ServiceComponents.AspNetCore.Hosting.Items
{
    public class PipeItem
    {
        public string Key { get; }
        public Action<WebHostBuilderContext, IApplicationBuilder> Use { get; }

        public PipeItem(string key, Action<WebHostBuilderContext, IApplicationBuilder> use)
        {
            Key = key;
            Use = use;
        }
    }
}
