using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ServiceComponents.Api.Mediator;

namespace ServiceComponents.Infrastructure.Receivers
{
    public class RequestParser
    {
        private readonly JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.All, TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple };

        public async Task ParseAsync(
            string payload, 
            string typeName, 
            Func<ICommand, CancellationToken, Task> commandAction,
            Func<IQuery, CancellationToken, Task> queryAction,
            Func<IEvent, CancellationToken, Task> eventAction,
            CancellationToken cancellationToken = default)
        {
            // Get body
            if (string.IsNullOrEmpty(payload)) payload = "{}";
            
            // Deserialize object
            dynamic request = string.IsNullOrEmpty(typeName)
                ? JsonConvert.DeserializeObject(payload, _jsonSerializerSettings) ??
                  throw new InvalidOperationException("Unable to deserialize request")
                : JsonConvert.DeserializeObject(payload, Type.GetType(typeName));

            switch (request) {
                case ICommand command:
                    await commandAction(command, cancellationToken);
                    break;

                case IQuery query:
                    await queryAction(query, cancellationToken);
                    break;

                case IEvent @event:
                    await eventAction(@event, cancellationToken);
                    break;

                default:
                    throw new InvalidOperationException($"Unknown request type: {request.GetType().FullName} (not a command, query or event)");
            }
        }
    }
}