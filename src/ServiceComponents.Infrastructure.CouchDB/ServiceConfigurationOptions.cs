using System;

namespace ServiceComponents.Infrastructure.CouchDB
{
    public class ServiceConfigurationOptions
    {
        public Uri Uri { get; set; }
        public string Database { get; set; }
        public string Document { get; set; }
    }
}