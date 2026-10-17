using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ServiceComponents.Api.Mediator;

namespace ServiceComponents.Test;

/// <summary>
/// Verbatim copies of the code as it was before the performance work (commit 377a47f), used as oracles:
/// the differential tests feed the same inputs to these and to the current code and require identical results.
/// </summary>
internal static class LegacyImplementations
{
    // ServiceComponents.Core.Extensions.TypeExtensions.AssemblyVersionlessQualifiedName
    public static string AssemblyVersionlessQualifiedName(object obj)
    {
        var type = obj.GetType();
        return $"{type.FullName}, {type.Assembly.GetName().Name}";
    }

    // ServiceComponents.Infrastructure.Monitoring.MetricExtensionMethods.MetricValues
    public static IEnumerable<string> MetricValues(object obj)
    {
        var type = obj.GetType();
        return type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(info => info.GetValue(obj).ToString());
    }

    // ServiceComponents.Infrastructure.Receivers.RequestParser. Note: Type.GetType resolves names without an
    // assembly against the calling assembly, which is the test assembly here, so only assembly-qualified names
    // and core library names are compared against this copy.
    public class RequestParser
    {
        private readonly JsonSerializerSettings _jsonSerializerSettings = new() { TypeNameHandling = TypeNameHandling.All, TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple };

        public async Task ParseAsync(
            string payload,
            string typeName,
            Func<ICommand, CancellationToken, Task> commandAction,
            Func<IQuery, CancellationToken, Task> queryAction,
            Func<IEvent, CancellationToken, Task> eventAction,
            Func<string, CancellationToken, Task> messageAction = null,
            CancellationToken cancellationToken = default)
        {
            // Get body
            if (string.IsNullOrEmpty(payload)) {
                payload = "{}";
            }

            // Deserialize object
            dynamic request;
            try {

                request = string.IsNullOrEmpty(typeName)
                    ? JsonConvert.DeserializeObject(payload, _jsonSerializerSettings) ??
                      throw new InvalidOperationException("Unable to deserialize request")
                    : JsonConvert.DeserializeObject(payload, Type.GetType(typeName));
            }
            catch (JsonReaderException) {
                request = String.Empty;
            }

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

                    if (messageAction != null) {
                        await messageAction(payload, cancellationToken);
                        break;
                    }

                    throw new InvalidOperationException($"Unknown request type: {request.GetType().FullName} (not a command, query or event)");
            }
        }
    }
}
