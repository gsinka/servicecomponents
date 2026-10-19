using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ServiceComponents.Core.Exceptions;

namespace ServiceComponents.AspNet.Exceptions
{
    /// <summary>
    /// Recognizes the exceptions of DotNet.ErrorHandler by their full type name (without referencing the package),
    /// reads their metadata from Exception.Data and converts it to <see cref="ErrorHandlerErrorResponse"/>
    /// </summary>
    internal static class ErrorHandlerExceptionInfo
    {
        public const string KeyMarker = "@ERROR_";

        /// <summary>
        /// Status code of the exceptions to be sent as <see cref="ErrorHandlerErrorResponse"/>, null for any other exception
        /// </summary>
        public static int? GetStatusCode(Exception exception)
        {
            for (var type = exception.GetType(); type != null; type = type.BaseType) {
                switch (type.FullName) {
                    case "DotNet.ErrorHandler.BusinessException": return (int)HttpStatusCode.BadRequest;
                    case "DotNet.ErrorHandler.NotFoundException": return (int)HttpStatusCode.NotFound;
                    case "DotNet.ErrorHandler.ConflictException": return (int)HttpStatusCode.Conflict;
                }
            }

            // The exception restored on the BFF is a Core exception, only its metadata shows that it came from DotNet.ErrorHandler
            if (!HasMetadata(exception)) {
                return null;
            }

            return exception switch {
                NotFoundException => (int)HttpStatusCode.NotFound,
                ConflictException => (int)HttpStatusCode.Conflict,
                GoneException => (int)HttpStatusCode.Gone,
                BusinessException => (int)HttpStatusCode.BadRequest,
                _ => null
            };
        }

        public static ErrorHandlerErrorResponse CreateResponse(Exception exception, int statusCode)
        {
            var data = GetData(exception);

            return new ErrorHandlerErrorResponse {
                Status = statusCode,
                Detail = exception.Message,
                Title = GetText(data, "TITLE"),
                ErrorCode = GetText(data, "ERROR_CODE"),
                ContextCode = GetText(data, "CONTEXT"),
                Hint = GetText(data, "HINT"),
                Timestamp = GetText(data, "UTC_TIMESTAMP"),
                TraceId = GetText(data, "TRACEID"),
                IServiceErrorObject = GetServiceError(exception),
                ExecutionSteps = GetToken(data, "EXECUTION_STEPS"),
                HelpUrls = GetToken(data, "HELP_URLS"),
                Data = data
            };
        }

        public static IDictionary<string, JToken> GetData(Exception exception)
        {
            var result = new Dictionary<string, JToken>();

            foreach (DictionaryEntry entry in exception.Data) {
                if (entry.Key is not string key || !IsMetadataKey(key) || entry.Value == null) {
                    continue;
                }

                try {
                    result[key] = entry.Value switch {
                        // The iService error keeps its extra fields on the way to the BFF
                        Exception inner when IsServiceErrorKey(key) => JObject.FromObject(CreateServiceError(inner)),
                        Exception inner => JToken.FromObject(inner.Message),
                        var value => JToken.FromObject(value)
                    };
                }
                catch (Exception) {
                    result[key] = JToken.FromObject(entry.Value.ToString() ?? string.Empty);
                }
            }

            return result;
        }

        public static void SetData(Exception exception, IDictionary<string, JToken> data)
        {
            foreach (var (key, value) in data) {
                if (!IsMetadataKey(key)) {
                    continue;
                }

                if (IsServiceErrorKey(key) && value is JObject serviceError) {
                    exception.Data[key] = RestoreServiceError(serviceError);
                    continue;
                }

                exception.Data[key] = ToDataValue(value);
            }
        }

        // Exception.Data only accepts serializable values: keep the simple ones as they are, the rest as JSON text
        private static object ToDataValue(JToken value)
        {
            return value.Type switch {
                JTokenType.String => value.Value<string>(),
                JTokenType.Date => value.Value<DateTime>(),
                JTokenType.Integer => value.Value<long>(),
                JTokenType.Float => value.Value<double>(),
                JTokenType.Boolean => value.Value<bool>(),
                _ => value.ToString(Formatting.None)
            };
        }

        private static bool IsServiceErrorKey(string key) => key.EndsWith(KeyMarker + "ISERVICE_ERROR", StringComparison.Ordinal);

        // The iService error is an exception again, its fields are the entries of its Exception.Data
        private static Exception RestoreServiceError(JObject serviceError)
        {
            var inner = new Exception(serviceError.Value<string>("text"));

            foreach (var property in serviceError.Properties()) {
                if (property.Name != "text") {
                    inner.Data[property.Name] = ToDataValue(property.Value);
                }
            }

            return inner;
        }

        private static bool HasMetadata(Exception exception)
        {
            foreach (DictionaryEntry entry in exception.Data) {
                if (entry.Key is string key && IsMetadataKey(key)) {
                    return true;
                }
            }

            return false;
        }

        private static bool IsMetadataKey(string key) => key.Contains(KeyMarker, StringComparison.Ordinal);

        // The metadata is found by the part after the marker (e.g. TITLE), whatever the prefix of the key is
        private static JToken GetToken(IDictionary<string, JToken> data, string name)
        {
            foreach (var (key, value) in data) {
                if (key.EndsWith(KeyMarker + name, StringComparison.Ordinal)) {
                    return value;
                }
            }

            return null;
        }

        private static string GetText(IDictionary<string, JToken> data, string name)
        {
            return GetToken(data, name) switch {
                null => null,
                { Type: JTokenType.String } token => token.Value<string>(),
                { Type: JTokenType.Date } token => token.Value<DateTime>().ToUniversalTime().ToString("o", CultureInfo.InvariantCulture),
                var token => token.ToString(Formatting.None)
            };
        }

        // The inner (service) error is kept as an exception in Exception.Data
        private static ErrorHandlerServiceError GetServiceError(Exception exception)
        {
            foreach (DictionaryEntry entry in exception.Data) {
                if (entry.Key is not string key || !IsServiceErrorKey(key)) {
                    continue;
                }

                return entry.Value switch {
                    null => null,
                    Exception inner => CreateServiceError(inner),
                    var value => new ErrorHandlerServiceError { Text = value.ToString() }
                };
            }

            return null;
        }

        /// <summary>
        /// The message of the inner exception is the text, everything else goes to the frontend unchanged:
        /// the simple public properties of the exception (e.g. Hint, Links) and the entries of its Exception.Data
        /// </summary>
        private static ErrorHandlerServiceError CreateServiceError(Exception inner)
        {
            var serviceError = new ErrorHandlerServiceError { Text = inner.Message };
            var fields = serviceError.Extra;

            foreach (var property in inner.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
                if (property.DeclaringType == typeof(Exception) || property.GetIndexParameters().Length > 0) {
                    continue;
                }

                var type = property.PropertyType;
                if ((type.IsPrimitive || type == typeof(string) || type == typeof(decimal)) && property.GetValue(inner) is { } value) {
                    fields[char.ToLowerInvariant(property.Name[0]) + property.Name[1..]] = JToken.FromObject(value);
                }
            }

            foreach (DictionaryEntry entry in inner.Data) {
                if (entry.Key is not string key || entry.Value == null) {
                    continue;
                }

                // The hint of a DotNet.ErrorHandler metadata is the hint, any other metadata is not part of the iService error
                if (IsMetadataKey(key)) {
                    key = key.EndsWith(KeyMarker + "HINT", StringComparison.Ordinal) ? "hint" : null;
                }

                if (key != null) {
                    fields[key] = ToToken(entry.Value);
                }
            }

            if (fields.Remove("text", out var text)) {
                serviceError.Text = text.Value<string>();
            }

            if (fields.Remove("hint", out var hint)) {
                serviceError.Hint = hint.Value<string>();
            }

            return serviceError;
        }

        private static JToken ToToken(object value)
        {
            try {
                return JToken.FromObject(value);
            }
            catch (Exception) {
                return JToken.FromObject(value.ToString() ?? string.Empty);
            }
        }
    }
}
