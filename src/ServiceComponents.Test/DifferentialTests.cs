using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using Newtonsoft.Json;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Application.Mediator;
using ServiceComponents.Core.Extensions;
using ServiceComponents.Infrastructure.Monitoring;
using ServiceComponents.Infrastructure.Receivers;
using Xunit;

namespace ServiceComponents.Test;

/// <summary>
/// Differential tests: the current code and a verbatim copy of the original code (<see cref="LegacyImplementations"/>)
/// get the same, large set of inputs, and every result (value, type identity or exception) must be identical.
/// Each input is run twice against the current code, so a cache hit has to give the same answer as a cache miss.
/// </summary>
public class DifferentialTests
{
    private static readonly string[] ApplicationAssemblyPrefixes = { "ServiceComponents", "ReferenceApplication" };

    private static IEnumerable<Assembly> ApplicationAssemblies()
    {
        // Make sure every library assembly is loaded, not just the ones touched so far.
        foreach (var type in new[] {
                     typeof(ICommand), typeof(ServiceComponents.Core.Extensions.TypeExtensions), typeof(IHandleCommand<>), typeof(RequestParser),
                     typeof(Infrastructure.Rabbit.RabbitConsumer), typeof(AspNet.RequestBinder), typeof(ReferenceApplication.Api.TestCommand)
                 }) {
            _ = type.Assembly;
        }

        return AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly => !assembly.IsDynamic && ApplicationAssemblyPrefixes.Any(prefix => assembly.GetName().Name!.StartsWith(prefix)))
            .OrderBy(assembly => assembly.GetName().Name);
    }

    private static Type[] LoadableTypes(Assembly assembly)
    {
        try {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception) {
            return exception.Types.Where(type => type != null).ToArray();
        }
    }

    private static Assembly LoadCopyIntoNewContext(Assembly original, bool collectible)
    {
        var directory = Path.Combine(Path.GetTempPath(), "sc-differential-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var copy = Path.Combine(directory, Path.GetFileName(original.Location));
        File.Copy(original.Location, copy);
        return new AssemblyLoadContext("differential-" + Guid.NewGuid().ToString("N"), collectible).LoadFromAssemblyPath(copy);
    }

    // --- AssemblyVersionlessQualifiedName ---

    [Fact]
    public void Type_name_matches_the_original_for_every_application_type_and_sample_framework_types()
    {
        var instances = new List<object>();

        foreach (var type in ApplicationAssemblies().SelectMany(LoadableTypes)) {
            if (type.IsAbstract || type.IsInterface || type.ContainsGenericParameters || type.IsByRefLike) {
                continue;
            }

            try {
                var instance = RuntimeHelpers.GetUninitializedObject(type);
                GC.SuppressFinalize(instance);
                instances.Add(instance);
            }
            catch (Exception) {
                // Types that cannot exist without a constructor (e.g. some runtime-special types) are skipped.
            }
        }

        // Generic, nested, array and value types, and types from a dynamic and a collectible assembly.
        instances.AddRange(new object[] {
            42, 4.2m, Guid.Empty, DateTime.MinValue, "text", new int[0], new ReferenceApplication.Api.TestCommand[0], new int[1, 1],
            new List<int>(), new Dictionary<string, ReferenceApplication.Api.TestCommand>(), new KeyValuePair<int, string>(1, "a"),
            new List<List<ReferenceApplication.Api.TestQuery>>(), Tuple.Create(1, "a"), (1, "a"),
            new TypeNameAndParserCharacterizationTests.Envelope<int>("c", 1),
            new TypeNameAndParserCharacterizationTests.Envelope<TypeNameAndParserCharacterizationTests.Envelope<string>>("c", null)
        });

        var dynamicAssembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("DifferentialDynamic"), AssemblyBuilderAccess.Run);
        var dynamicType = dynamicAssembly.DefineDynamicModule("DifferentialDynamic").DefineType("Dynamic.Message", TypeAttributes.Public).CreateType();
        instances.Add(Activator.CreateInstance(dynamicType));

        var collectible = LoadCopyIntoNewContext(typeof(ReferenceApplication.Api.TestCommand).Assembly, collectible: true);
        instances.Add(RuntimeHelpers.GetUninitializedObject(collectible.GetType("ReferenceApplication.Api.TestCommand", throwOnError: true)!));

        foreach (var instance in instances) {
            var expected = LegacyImplementations.AssemblyVersionlessQualifiedName(instance);
            Assert.Equal(expected, instance.AssemblyVersionlessQualifiedName());
            Assert.Equal(expected, instance.AssemblyVersionlessQualifiedName());
        }

        Assert.True(instances.Count > 300, $"only {instances.Count} instances were checked");
    }

    // --- RequestParser ---

    private static readonly Regex Guids = new(@"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}", RegexOptions.Compiled);

    private delegate Task Parse(string payload, string typeName,
        Func<ICommand, CancellationToken, Task> commandAction, Func<IQuery, CancellationToken, Task> queryAction,
        Func<IEvent, CancellationToken, Task> eventAction, Func<string, CancellationToken, Task> messageAction, CancellationToken cancellationToken);

    private static string Json(object value)
    {
        try {
            return Guids.Replace(JsonConvert.SerializeObject(value), "<guid>");
        }
        catch (Exception exception) {
            return $"<not serializable: {exception.GetType().Name}>";
        }
    }

    private static async Task<string> Outcome(Parse parse, string payload, string typeName, bool withMessageAction)
    {
        var calls = new List<string>();
        try {
            await parse(payload, typeName,
                (command, _) => { calls.Add($"command {command.GetType().AssemblyQualifiedName} {Json(command)}"); return Task.CompletedTask; },
                (query, _) => { calls.Add($"query {query.GetType().AssemblyQualifiedName} {Json(query)}"); return Task.CompletedTask; },
                (@event, _) => { calls.Add($"event {@event.GetType().AssemblyQualifiedName} {Json(@event)}"); return Task.CompletedTask; },
                withMessageAction ? (message, _) => { calls.Add($"message {message}"); return Task.CompletedTask; } : null,
                CancellationToken.None);
            return string.Join(" | ", calls);
        }
        catch (Exception exception) {
            return $"{string.Join(" | ", calls)} throws {exception.GetType().FullName}: {Guids.Replace(exception.Message, "<guid>")}";
        }
    }

    public static IEnumerable<string> QualifiedTypeNames()
    {
        foreach (var type in ApplicationAssemblies().SelectMany(LoadableTypes).Where(type => type.IsPublic || type.IsNestedPublic)) {
            if (type.ContainsGenericParameters) {
                continue;
            }
            yield return $"{type.FullName}, {type.Assembly.GetName().Name}";
            yield return type.AssemblyQualifiedName;
        }

        var testCommand = "ReferenceApplication.Api.TestCommand";
        yield return $"{testCommand}, REFERENCEAPPLICATION.API";
        yield return $"{testCommand},ReferenceApplication.Api";
        yield return $" {testCommand}, ReferenceApplication.Api";
        yield return $"{testCommand}, ReferenceApplication.Api, Version=99.0.0.0";
        yield return $"{testCommand}, ReferenceApplication.Api, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
        yield return "ReferenceApplication.Api.testcommand, ReferenceApplication.Api";
        yield return $"System.Collections.Generic.List`1[[{testCommand}, ReferenceApplication.Api]], System.Private.CoreLib";
        yield return "System.Collections.Generic.Dictionary`2[[System.String],[System.Int32]]";
        yield return "System.Collections.Generic.List`1[[System.Int32]]";
        yield return "System.String";
        yield return "System.String, System.Private.CoreLib";
        yield return "System.Int32";
        yield return "system.string";
        yield return "System.Object";
        yield return "Nope";
        yield return "Nope, Nope";
        yield return "Nope.Nope, ReferenceApplication.Api";
        yield return "[[";
        yield return "System.Collections.Generic.List`1[[";
        yield return ",";
        yield return " ";
        yield return "";
        yield return null;
    }

    private static readonly string[] Payloads = {
        null, "", "{}", "null", "[]", "{", "\"text\"", "42",
        "{\"CommandId\":\"c-1\",\"QueryId\":\"q-1\",\"EventId\":\"e-1\",\"Data\":\"d\",\"Name\":\"n\"}",
        "{\"$type\":\"ReferenceApplication.Api.TestCommand, ReferenceApplication.Api\",\"Data\":\"x\"}",
        "{\"$type\":\"ReferenceApplication.Api.TestQuery, ReferenceApplication.Api\",\"Data\":\"x\"}",
        "{\"$type\":\"System.Collections.Generic.List`1[[System.Int32, System.Private.CoreLib]], System.Private.CoreLib\",\"$values\":[1,2]}"
    };

    [Fact]
    public async Task Request_parser_matches_the_original_for_every_type_name_and_payload()
    {
        var legacy = new LegacyImplementations.RequestParser();
        var compared = 0;
        var mismatches = new List<string>();

        foreach (var typeName in QualifiedTypeNames().Distinct()) {
            foreach (var payload in Payloads) {
                foreach (var withMessageAction in new[] { false, true }) {
                    var expected = await Outcome((p, t, c, q, e, m, ct) => legacy.ParseAsync(p, t, c, q, e, m, ct), payload, typeName, withMessageAction);
                    var first = await Outcome((p, t, c, q, e, m, ct) => new RequestParser().ParseAsync(p, t, c, q, e, m, ct), payload, typeName, withMessageAction);
                    var second = await Outcome((p, t, c, q, e, m, ct) => new RequestParser().ParseAsync(p, t, c, q, e, m, ct), payload, typeName, withMessageAction);

                    if (expected != first || expected != second) {
                        mismatches.Add($"type '{typeName}', payload '{payload}', message action {withMessageAction}:\n  original: {expected}\n  current:  {first}\n  again:    {second}");
                    }
                    compared++;
                }
            }
        }

        Assert.True(mismatches.Count == 0, string.Join("\n", mismatches.Take(10)));
        Assert.True(compared > 5000, $"only {compared} cases were compared");
    }

    // --- ClosedGenericTypes (internal; reached through reflection on purpose) ---

    // The cache does not exist before the performance work; the same test then compares MakeGenericType with itself,
    // so this file runs unchanged against both versions.
    private static readonly Type ClosedGenericTypes = typeof(RequestParser).Assembly.GetType("ServiceComponents.Infrastructure.Mediator.ClosedGenericTypes");

    private static string Invoke(string method, params Type[] arguments)
    {
        if (ClosedGenericTypes == null) {
            var typeArguments = arguments.Skip(1).ToArray();
            return Original(() => method == "Close"
                ? arguments[0].MakeGenericType(typeArguments)
                : typeof(IEnumerable<>).MakeGenericType(arguments[0].MakeGenericType(typeArguments)));
        }

        try {
            var result = (Type)ClosedGenericTypes.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Single(candidate => candidate.Name == method && candidate.GetParameters().Length == arguments.Length)
                .Invoke(null, arguments.Cast<object>().ToArray());
            return $"{RuntimeHelpers.GetHashCode(result)} {result}";
        }
        catch (TargetInvocationException exception) {
            return $"throws {exception.InnerException!.GetType().FullName}: {exception.InnerException.Message}";
        }
    }

    private static string Original(Func<Type> build)
    {
        try {
            var result = build();
            return $"{RuntimeHelpers.GetHashCode(result)} {result}";
        }
        catch (Exception exception) {
            return $"throws {exception.GetType().FullName}: {exception.Message}";
        }
    }

    [Fact]
    public void Closed_generic_types_match_MakeGenericType_including_identity_and_errors()
    {
        var single = new[] {
            typeof(IHandleCommand<>), typeof(IHandleEvent<>), typeof(IPreHandleCommand<>), typeof(IPostHandleCommand<>), typeof(IHandleCommandFailure<>),
            typeof(IPreHandleEvent<>), typeof(IPostHandleEvent<>), typeof(IHandleEventFailure<>), typeof(IValidator<>)
        };
        var pair = new[] { typeof(IHandleQuery<,>), typeof(IPreHandleQuery<,>), typeof(IPostHandleQuery<,>), typeof(IHandleQueryFailure<,>) };

        var collectible = LoadCopyIntoNewContext(typeof(ReferenceApplication.Api.TestCommand).Assembly, collectible: true);
        var arguments = ApplicationAssemblies().SelectMany(LoadableTypes)
            .Where(type => !type.ContainsGenericParameters && (typeof(ICommand).IsAssignableFrom(type) || typeof(IQuery).IsAssignableFrom(type) || typeof(IEvent).IsAssignableFrom(type)))
            .Concat(new[] { typeof(string), typeof(int), typeof(object), typeof(List<int>), collectible.GetType("ReferenceApplication.Api.TestCommand")!, collectible.GetType("ReferenceApplication.Api.TestQuery")! })
            .ToArray();
        var results = new[] { typeof(string), typeof(int), typeof(object), typeof(List<ReferenceApplication.Api.TestCommand>) };

        var compared = 0;
        foreach (var argument in arguments) {
            foreach (var definition in single) {
                var expected = Original(() => definition.MakeGenericType(argument));
                Assert.Equal(expected, Invoke("Close", definition, argument));
                Assert.Equal(expected, Invoke("Close", definition, argument));

                var expectedEnumerable = Original(() => typeof(IEnumerable<>).MakeGenericType(definition.MakeGenericType(argument)));
                Assert.Equal(expectedEnumerable, Invoke("EnumerableOf", definition, argument));
                Assert.Equal(expectedEnumerable, Invoke("EnumerableOf", definition, argument));
                compared += 2;
            }

            foreach (var definition in pair) {
                foreach (var result in results) {
                    var expected = Original(() => definition.MakeGenericType(argument, result));
                    Assert.Equal(expected, Invoke("Close", definition, argument, result));
                    Assert.Equal(expected, Invoke("Close", definition, argument, result));

                    var expectedEnumerable = Original(() => typeof(IEnumerable<>).MakeGenericType(definition.MakeGenericType(argument, result)));
                    Assert.Equal(expectedEnumerable, Invoke("EnumerableOf", definition, argument, result));
                    Assert.Equal(expectedEnumerable, Invoke("EnumerableOf", definition, argument, result));
                    compared += 2;
                }
            }
        }

        Assert.True(compared > 500, $"only {compared} combinations were compared");
    }

    // --- MetricValues ---

    public class GetterCounter
    {
        private int _reads;
        public int Reads => _reads;
        public string Value { get { Interlocked.Increment(ref _reads); return "v"; } }
    }

    private static string Values(Func<IEnumerable<string>> values)
    {
        try {
            return string.Join(", ", values().ToList());
        }
        catch (Exception exception) {
            return $"throws {exception.GetType().FullName} ({exception.InnerException?.GetType().FullName}): {exception.InnerException?.Message ?? exception.Message}";
        }
    }

    [Fact]
    public void Metric_values_match_the_original_for_metrics_and_arbitrary_objects()
    {
        var command = new ReferenceApplication.Api.TestCommand("d");
        var collectible = LoadCopyIntoNewContext(typeof(ReferenceApplication.Api.TestCommand).Assembly, collectible: true);
        var objects = new List<object> {
            new RequestCounterMetric(command), new RequestDurationMetric(command), new RequestFailureMetric(command, new TimeoutException()),
            new RequestCounterMetric(new ReferenceApplication.Api.TestQuery("q")), new RequestGaugeMetric("n", "t"),
            new PrometheusMetricsCharacterizationTests.SampleCounter("a", "b", 3), new PrometheusMetricsCharacterizationTests.NullLabelCounter(null),
            new PrometheusMetricsCharacterizationTests.ThrowingLabelCounter(), new PrometheusMetricsCharacterizationTests.UnannotatedMetric(),
            new { A = 1, B = "x", C = (string)null }, new { }, 42, "text", Guid.Empty, new List<int> { 1 }, new KeyValuePair<string, int>("k", 1),
            command, Activator.CreateInstance(collectible.GetType("ReferenceApplication.Api.TestCommand")!, "collectible")!
        };

        foreach (var value in objects) {
            var expected = Values(() => LegacyImplementations.MetricValues(value));
            Assert.Equal(expected, Values(() => value.MetricValues()));
            Assert.Equal(expected, Values(() => value.MetricValues()));
        }

        // Laziness and getter calls: each enumeration reads every property once, as before.
        var original = new GetterCounter();
        var current = new GetterCounter();
        var originalValues = LegacyImplementations.MetricValues(original);
        var currentValues = current.MetricValues();
        Assert.Equal(0, original.Reads);
        Assert.Equal(0, current.Reads);
        originalValues.ToList();
        originalValues.ToList();
        currentValues.ToList();
        currentValues.ToList();
        Assert.Equal(original.Reads, current.Reads);

        Assert.Throws<NullReferenceException>(() => LegacyImplementations.MetricValues(null));
        Assert.Throws<NullReferenceException>(() => ((object)null).MetricValues());
    }

    // --- UTF-8 decoding: the old ToArray() copy and the new Span read give the same string for any bytes ---

    [Fact]
    public void Utf8_decoding_of_a_span_equals_decoding_of_a_copied_array_for_random_and_malformed_bytes()
    {
        var random = new Random(20261017);
        var samples = new List<byte[]> {
            Array.Empty<byte>(), new byte[] { 0xEF, 0xBB, 0xBF, 0x41 }, new byte[] { 0xC0, 0x80 }, new byte[] { 0xED, 0xA0, 0x80 },
            new byte[] { 0xF4, 0x90, 0x80, 0x80 }, new byte[] { 0xE2, 0x82 }, new byte[] { 0x80, 0xBF, 0xFE, 0xFF }, new byte[] { 0x00, 0x7F },
            Encoding.UTF8.GetBytes("Árvíztűrő tükörfúrógép – 日本語 – 🚀")
        };
        for (var i = 0; i < 20000; i++) {
            var bytes = new byte[random.Next(0, 64)];
            random.NextBytes(bytes);
            samples.Add(bytes);
        }

        foreach (var bytes in samples) {
            var body = new ReadOnlyMemory<byte>(bytes);
            Assert.Equal(Encoding.UTF8.GetString(body.ToArray()), Encoding.UTF8.GetString(body.Span));
        }
    }
}
