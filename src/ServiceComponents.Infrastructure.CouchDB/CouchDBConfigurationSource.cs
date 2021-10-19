using System.IO;
using System.Net.Http;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Newtonsoft.Json.Linq;

namespace ServiceComponents.Infrastructure.CouchDB
{
    public class CouchDbConfigurationSource : JsonStreamConfigurationSource
    {
        private readonly ServiceConfigurationOptions _options;

        public CouchDbConfigurationSource(ServiceConfigurationOptions options)
        {
            _options = options;
        }

        public override IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            using var httpClient = new HttpClient() { BaseAddress = _options.Uri };
            var response = httpClient.GetAsync($"{_options.Database}/{_options.Document}").Result.Content.ReadAsStringAsync().Result;

            var json = JObject.Parse(response);
            json.Remove("_id");
            json.Remove("_rev");

            Stream = new MemoryStream(Encoding.UTF8.GetBytes(json.ToString()));
            return new JsonStreamConfigurationProvider(this);
        }
    }
}
