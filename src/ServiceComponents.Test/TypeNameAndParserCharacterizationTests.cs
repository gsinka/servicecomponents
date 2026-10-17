using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Core.Extensions;
using ServiceComponents.Infrastructure.Receivers;
using Xunit;

namespace ServiceComponents.Test;

/// <summary>
/// Pins the wire type name (domain-type header / RabbitMQ Type) and how RequestParser turns a payload plus
/// type name into a command, query, event or raw message. Must pass before and after the performance work.
/// </summary>
public class TypeNameAndParserCharacterizationTests
{
    public class CreateItem : ICommand
    {
        public string CommandId { get; }
        public string Name { get; }

        public CreateItem(string commandId, string name)
        {
            CommandId = commandId;
            Name = name;
        }
    }

    public class GetItem : IQuery<string>
    {
        public string QueryId { get; }
        public int Id { get; }

        public GetItem(string queryId, int id)
        {
            QueryId = queryId;
            Id = id;
        }
    }

    public class ItemCreated : IEvent
    {
        public string EventId { get; }
        public string Name { get; }

        public ItemCreated(string eventId, string name)
        {
            EventId = eventId;
            Name = name;
        }
    }

    public class Envelope<T> : ICommand
    {
        public string CommandId { get; }
        public T Payload { get; }

        public Envelope(string commandId, T payload)
        {
            CommandId = commandId;
            Payload = payload;
        }
    }

    // --- AssemblyVersionlessQualifiedName ---

    [Fact]
    public void Type_name_is_full_name_and_assembly_name_without_version()
    {
        var name = new CreateItem("c", "n").AssemblyVersionlessQualifiedName();

        Assert.Equal("ServiceComponents.Test.TypeNameAndParserCharacterizationTests+CreateItem, ServiceComponents.Test", name);
    }

    [Fact]
    public void Type_name_of_generic_type_keeps_assembly_qualified_type_arguments()
    {
        var name = new Envelope<int>("c", 1).AssemblyVersionlessQualifiedName();

        Assert.Equal($"{typeof(Envelope<int>).FullName}, ServiceComponents.Test", name);
        Assert.StartsWith("ServiceComponents.Test.TypeNameAndParserCharacterizationTests+Envelope`1[[System.Int32, System.Private.CoreLib, Version=", name);
    }

    [Fact]
    public void Type_name_is_stable_across_calls_and_instances()
    {
        var first = new CreateItem("a", "b").AssemblyVersionlessQualifiedName();
        var second = new CreateItem("c", "d").AssemblyVersionlessQualifiedName();

        Assert.Equal(first, second);
        Assert.Equal("System.String, System.Private.CoreLib", "text".AssemblyVersionlessQualifiedName());
        Assert.Equal("System.Int32, System.Private.CoreLib", 42.AssemblyVersionlessQualifiedName());
    }

    [Fact]
    public void Type_name_of_null_throws_null_reference()
    {
        Assert.Throws<NullReferenceException>(() => ((object)null).AssemblyVersionlessQualifiedName());
    }

    // --- RequestParser ---

    private sealed class Calls
    {
        public readonly List<object> Commands = new();
        public readonly List<object> Queries = new();
        public readonly List<object> Events = new();
        public readonly List<string> Messages = new();

        public Task Parse(string payload, string typeName, bool withMessageAction = false) =>
            new RequestParser().ParseAsync(
                payload,
                typeName,
                (command, _) => { Commands.Add(command); return Task.CompletedTask; },
                (query, _) => { Queries.Add(query); return Task.CompletedTask; },
                (@event, _) => { Events.Add(@event); return Task.CompletedTask; },
                withMessageAction ? (message, _) => { Messages.Add(message); return Task.CompletedTask; } : null,
                CancellationToken.None);
    }

    private static string TypeName<T>() => $"{typeof(T).FullName}, {typeof(T).Assembly.GetName().Name}";

    [Fact]
    public async Task Typed_command_query_and_event_go_to_their_actions()
    {
        var calls = new Calls();

        await calls.Parse("{\"CommandId\":\"c-1\",\"Name\":\"chair\"}", TypeName<CreateItem>());
        await calls.Parse("{\"QueryId\":\"q-1\",\"Id\":7}", TypeName<GetItem>());
        await calls.Parse("{\"EventId\":\"e-1\",\"Name\":\"chair\"}", TypeName<ItemCreated>());

        var command = Assert.IsType<CreateItem>(Assert.Single(calls.Commands));
        Assert.Equal(("c-1", "chair"), (command.CommandId, command.Name));
        var query = Assert.IsType<GetItem>(Assert.Single(calls.Queries));
        Assert.Equal(("q-1", 7), (query.QueryId, query.Id));
        var @event = Assert.IsType<ItemCreated>(Assert.Single(calls.Events));
        Assert.Equal(("e-1", "chair"), (@event.EventId, @event.Name));
    }

    [Fact]
    public async Task Same_type_name_twice_creates_two_independent_instances()
    {
        var calls = new Calls();

        await calls.Parse("{\"CommandId\":\"c-1\",\"Name\":\"one\"}", TypeName<CreateItem>());
        await calls.Parse("{\"CommandId\":\"c-2\",\"Name\":\"two\"}", TypeName<CreateItem>());

        Assert.Equal(2, calls.Commands.Count);
        Assert.NotSame(calls.Commands[0], calls.Commands[1]);
        Assert.Equal("two", ((CreateItem)calls.Commands[1]).Name);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task Empty_payload_is_treated_as_empty_object(string payload)
    {
        var calls = new Calls();

        await calls.Parse(payload, TypeName<CreateItem>());

        var command = Assert.IsType<CreateItem>(Assert.Single(calls.Commands));
        Assert.Null(command.CommandId);
        Assert.Null(command.Name);
    }

    [Fact]
    public async Task Without_type_name_the_embedded_type_is_used()
    {
        var calls = new Calls();
        var payload = JsonConvert.SerializeObject(new CreateItem("c-9", "lamp"), new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All });

        await calls.Parse(payload, null);

        Assert.Equal("lamp", Assert.IsType<CreateItem>(Assert.Single(calls.Commands)).Name);
    }

    [Fact]
    public async Task Without_any_type_information_it_is_an_unknown_request()
    {
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => new Calls().Parse("{\"a\":1}", ""));

        Assert.Equal("Unknown request type: Newtonsoft.Json.Linq.JObject (not a command, query or event)", exception.Message);
    }

    [Theory]
    [InlineData("Does.Not.Exist, Nowhere")]
    [InlineData("Does.Not.Exist")]
    [InlineData("ServiceComponents.Test.TypeNameAndParserCharacterizationTests+CreateItem, Wrong.Assembly")]
    public async Task Unresolvable_type_name_is_an_unknown_request(string typeName)
    {
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => new Calls().Parse("{\"a\":1}", typeName));

        Assert.Equal("Unknown request type: Newtonsoft.Json.Linq.JObject (not a command, query or event)", exception.Message);
    }

    [Fact]
    public async Task Unresolvable_type_name_goes_to_message_action_with_original_payload()
    {
        var calls = new Calls();

        await calls.Parse("{\"a\":1}", "Does.Not.Exist, Nowhere", withMessageAction: true);

        Assert.Equal("{\"a\":1}", Assert.Single(calls.Messages));
        Assert.Empty(calls.Commands);
    }

    [Fact]
    public async Task Resolvable_type_that_is_not_a_message_is_an_unknown_request()
    {
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => new Calls().Parse("{\"a\":1}", typeof(Dictionary<string, int>).AssemblyQualifiedName));

        Assert.StartsWith("Unknown request type: System.Collections.Generic.Dictionary`2", exception.Message);
    }

    [Fact]
    public async Task Json_reader_error_becomes_a_raw_message()
    {
        var calls = new Calls();

        // A JSON object where a string is expected fails in the reader, which the parser maps to a raw message.
        await calls.Parse("{\"a\":1}", "System.String", withMessageAction: true);

        Assert.Equal("{\"a\":1}", Assert.Single(calls.Messages));
    }

    [Fact]
    public async Task Json_reader_error_without_message_action_is_an_unknown_request()
    {
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => new Calls().Parse("{\"a\":1}", "System.String"));

        Assert.Equal("Unknown request type: System.String (not a command, query or event)", exception.Message);
    }

    [Fact]
    public async Task Truncated_json_for_a_known_type_throws_serialization_exception()
    {
        await Assert.ThrowsAsync<JsonSerializationException>(() => new Calls().Parse("{\"CommandId\":\"c-1\"", TypeName<CreateItem>()));
    }

    [Fact]
    public async Task Type_name_lookup_does_not_cache_failures()
    {
        var calls = new Calls();
        var typeName = TypeName<CreateItem>();

        await Assert.ThrowsAsync<InvalidOperationException>(() => calls.Parse("{}", typeName + "X"));
        await calls.Parse("{\"Name\":\"after\"}", typeName);
        await Assert.ThrowsAsync<InvalidOperationException>(() => calls.Parse("{}", typeName + "X"));

        Assert.Equal("after", ((CreateItem)Assert.Single(calls.Commands)).Name);
    }

    [Fact]
    public async Task Type_names_are_case_sensitive()
    {
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => new Calls().Parse("{}", TypeName<CreateItem>().ToLowerInvariant()));

        Assert.Contains("JObject", exception.Message);
    }

    [Fact]
    public async Task Names_without_an_assembly_resolve_against_the_infrastructure_assembly_and_the_core_library()
    {
        // Type.GetType looks in the calling assembly (ServiceComponents.Infrastructure) and the core library.
        var infrastructureType = await Assert.ThrowsAsync<InvalidOperationException>(() => new Calls().Parse("{}", "ServiceComponents.Infrastructure.Receivers.CorrelationLogOptions"));
        var coreType = await Assert.ThrowsAsync<InvalidOperationException>(() => new Calls().Parse("{}", "System.Object"));
        var otherAssemblyType = await Assert.ThrowsAsync<InvalidOperationException>(() => new Calls().Parse("{}", "ServiceComponents.Test.TypeNameAndParserCharacterizationTests+CreateItem"));

        Assert.Equal("Unknown request type: ServiceComponents.Infrastructure.Receivers.CorrelationLogOptions (not a command, query or event)", infrastructureType.Message);
        Assert.Equal("Unknown request type: Newtonsoft.Json.Linq.JObject (not a command, query or event)", coreType.Message);
        Assert.Equal("Unknown request type: Newtonsoft.Json.Linq.JObject (not a command, query or event)", otherAssemblyType.Message);
    }

    [Fact]
    public async Task Type_lookup_follows_the_contextual_reflection_load_context()
    {
        // A second copy of ReferenceApplication.Api in its own load context: the same name means another type there.
        var original = typeof(ReferenceApplication.Api.TestCommand).Assembly;
        var directory = Path.Combine(Path.GetTempPath(), "sc-contextual-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var copyPath = Path.Combine(directory, Path.GetFileName(original.Location));
        File.Copy(original.Location, copyPath);
        var context = new AssemblyLoadContext("characterization-" + Guid.NewGuid().ToString("N"));
        var copy = context.LoadFromAssemblyPath(copyPath);
        const string typeName = "ReferenceApplication.Api.TestCommand, ReferenceApplication.Api";

        var before = new Calls();
        await before.Parse("{\"Data\":\"default\"}", typeName);
        var inside = new Calls();
        using (context.EnterContextualReflection()) {
            await inside.Parse("{\"Data\":\"copy\"}", typeName);
        }
        var after = new Calls();
        await after.Parse("{\"Data\":\"default again\"}", typeName);

        Assert.Same(typeof(ReferenceApplication.Api.TestCommand), Assert.Single(before.Commands).GetType());
        Assert.Same(copy, Assert.Single(inside.Commands).GetType().Assembly);
        Assert.Same(typeof(ReferenceApplication.Api.TestCommand), Assert.Single(after.Commands).GetType());
    }

    [Fact]
    public async Task A_name_that_fails_now_resolves_once_its_assembly_can_be_found()
    {
        var assemblyName = "LateTypes" + Guid.NewGuid().ToString("N");
        var late = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(assemblyName), AssemblyBuilderAccess.Run);
        late.DefineDynamicModule(assemblyName).DefineType("Late.LateType", TypeAttributes.Public).CreateType();
        var available = false;
        ResolveEventHandler resolve = (_, args) => available && args.Name.StartsWith(assemblyName) ? late : null;
        AppDomain.CurrentDomain.AssemblyResolve += resolve;
        try {
            var typeName = $"Late.LateType, {assemblyName}";

            var before = await Assert.ThrowsAsync<InvalidOperationException>(() => new Calls().Parse("{}", typeName));
            available = true;
            var after = await Assert.ThrowsAsync<InvalidOperationException>(() => new Calls().Parse("{}", typeName));
            var again = await Assert.ThrowsAsync<InvalidOperationException>(() => new Calls().Parse("{}", typeName));

            Assert.Equal("Unknown request type: Newtonsoft.Json.Linq.JObject (not a command, query or event)", before.Message);
            Assert.Equal("Unknown request type: Late.LateType (not a command, query or event)", after.Message);
            Assert.Equal(after.Message, again.Message);
        }
        finally {
            AppDomain.CurrentDomain.AssemblyResolve -= resolve;
        }
    }

    [Fact]
    public async Task Parser_instances_do_not_share_results()
    {
        var first = new Calls();
        var second = new Calls();

        await first.Parse("{\"Name\":\"a\"}", TypeName<CreateItem>());
        await second.Parse("{\"Name\":\"b\"}", TypeName<CreateItem>());

        Assert.Equal("a", ((CreateItem)Assert.Single(first.Commands)).Name);
        Assert.Equal("b", ((CreateItem)Assert.Single(second.Commands)).Name);
        Assert.IsType<JObject>(JsonConvert.DeserializeObject("{}"));
    }
}
