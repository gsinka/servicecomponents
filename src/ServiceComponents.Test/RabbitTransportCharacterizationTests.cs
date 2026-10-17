using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Moq;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Serilog.Events;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Infrastructure.Rabbit;
using ServiceComponents.Infrastructure.Rabbit.Senders;
using Xunit;

namespace ServiceComponents.Test;

/// <summary>
/// Pins the RabbitMQ consumer and senders against an in-memory channel: what is decoded, acknowledged,
/// rejected, published (exchange, routing key, properties, body bytes), how query replies are matched and
/// how cancellation surfaces. Must pass before and after the performance work.
/// </summary>
public class RabbitTransportCharacterizationTests
{
    public class RabbitPing : ICommand
    {
        public string CommandId { get; }
        public string Text { get; }

        public RabbitPing(string commandId, string text)
        {
            CommandId = commandId;
            Text = text;
        }
    }

    public class RabbitLookup : IQuery<List<string>>
    {
        public string QueryId { get; }
        public string Term { get; }

        public RabbitLookup(string queryId, string term)
        {
            QueryId = queryId;
            Term = term;
        }
    }

    public class RabbitHappened : IEvent
    {
        public string EventId { get; }
        public string What { get; }

        public RabbitHappened(string eventId, string what)
        {
            EventId = eventId;
            What = what;
        }
    }

    private const string Unicode = "Árvíztűrő tükörfúrógép – 日本語 – 🚀";

    private static string TypeName<T>() => $"{typeof(T).FullName}, {typeof(T).Assembly.GetName().Name}";

    // Recording fakes (plain classes, so the consumer's dynamic dispatch binds exactly as in production)

    public class Received
    {
        public readonly List<object> Messages = new();
        public Exception Throw { get; set; }
    }

    public class FakeCommandReceiver : IReceiveRabbitCommand
    {
        private readonly Received _received;
        public FakeCommandReceiver(Received received) => _received = received;

        public Task ReceiveAsync<T>(T command, BasicDeliverEventArgs args, CancellationToken cancellationToken) where T : ICommand
        {
            _received.Messages.Add(command);
            return _received.Throw != null ? Task.FromException(_received.Throw) : Task.CompletedTask;
        }
    }

    public class FakeQueryReceiver : IReceiveRabbitQuery
    {
        private readonly Received _received;
        public FakeQueryReceiver(Received received) => _received = received;

        public Task<T> ReceiveAsync<T>(IQuery<T> query, BasicDeliverEventArgs args, CancellationToken cancellationToken)
        {
            _received.Messages.Add(query);
            object result = new List<string> { "first", Unicode };
            return Task.FromResult((T)result);
        }
    }

    public class FakeEventReceiver : IReceiveRabbitEvent
    {
        private readonly Received _received;
        public FakeEventReceiver(Received received) => _received = received;

        public Task ReceiveAsync<T>(T @event, BasicDeliverEventArgs args, CancellationToken cancellationToken) where T : IEvent
        {
            _received.Messages.Add(@event);
            return Task.CompletedTask;
        }
    }

    public class FakeMessageReceiver : IReceiveRabbitMessage
    {
        private readonly Received _received;
        public FakeMessageReceiver(Received received) => _received = received;

        public Task ReceiveAsync(string body, BasicDeliverEventArgs args, CancellationToken cancellationToken)
        {
            _received.Messages.Add(body);
            return Task.CompletedTask;
        }
    }

    private sealed class ChannelProbe
    {
        public readonly Mock<IChannel> Channel = new();
        public readonly List<(string exchange, string routingKey, bool mandatory, BasicProperties properties, byte[] body)> Published = new();
        public readonly List<(ulong tag, bool multiple)> Acks = new();
        public readonly List<(ulong tag, bool requeue)> Rejects = new();
        public readonly List<string> DeletedQueues = new();
        public IAsyncBasicConsumer Consumer;
        public Action<BasicProperties> AfterPublish;

        public ChannelProbe()
        {
            Channel.Setup(channel => channel.BasicConsumeAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<IDictionary<string, object>>(), It.IsAny<IAsyncBasicConsumer>(), It.IsAny<CancellationToken>()))
                .Callback<string, bool, string, bool, bool, IDictionary<string, object>, IAsyncBasicConsumer, CancellationToken>((_, _, _, _, _, _, consumer, _) => Consumer = consumer)
                .ReturnsAsync("ctag");
            Channel.Setup(channel => channel.QueueDeclareAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<IDictionary<string, object>>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new QueueDeclareOk("amq.gen-reply", 0, 0));
            Channel.Setup(channel => channel.QueueDeleteAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .Callback<string, bool, bool, bool, CancellationToken>((queue, _, _, _, _) => DeletedQueues.Add(queue))
                .ReturnsAsync(0u);
            Channel.Setup(channel => channel.BasicAckAsync(It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .Callback<ulong, bool, CancellationToken>((tag, multiple, _) => Acks.Add((tag, multiple)))
                .Returns(ValueTask.CompletedTask);
            Channel.Setup(channel => channel.BasicRejectAsync(It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .Callback<ulong, bool, CancellationToken>((tag, requeue, _) => Rejects.Add((tag, requeue)))
                .Returns(ValueTask.CompletedTask);
            Channel.Setup(channel => channel.BasicPublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<BasicProperties>(), It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, bool, BasicProperties, ReadOnlyMemory<byte>, CancellationToken>((exchange, routingKey, mandatory, properties, body, _) => {
                    Published.Add((exchange, routingKey, mandatory, properties, body.ToArray()));
                    AfterPublish?.Invoke(properties);
                })
                .Returns(ValueTask.CompletedTask);
        }

        public Task Reply(string correlationId, string json) =>
            Consumer.HandleBasicDeliverAsync("ctag", 99, false, "", "amq.gen-reply", new BasicProperties { CorrelationId = correlationId }, Encoding.UTF8.GetBytes(json));
    }

    // --- RabbitConsumer ---

    private static async Task<(ChannelProbe probe, Received received)> StartConsumer()
    {
        var received = new Received();
        var builder = new ContainerBuilder();
        builder.RegisterInstance(received);
        builder.RegisterType<FakeCommandReceiver>().As<IReceiveRabbitCommand>();
        builder.RegisterType<FakeQueryReceiver>().As<IReceiveRabbitQuery>();
        builder.RegisterType<FakeEventReceiver>().As<IReceiveRabbitEvent>();
        builder.RegisterType<FakeMessageReceiver>().As<IReceiveRabbitMessage>();
        var container = builder.Build();

        var probe = new ChannelProbe();
        var consumer = new RabbitConsumer(new LogCapture(LogEventLevel.Information).Logger, container, probe.Channel.Object, "orders", "consumer-tag");
        await consumer.StartAsync();
        Assert.NotNull(probe.Consumer);

        return (probe, received);
    }

    private static Task Deliver(ChannelProbe probe, ulong tag, string typeName, string json, string replyTo = null, string correlationId = null) =>
        probe.Consumer.HandleBasicDeliverAsync("consumer-tag", tag, false, "exchange", "orders",
            new BasicProperties { Type = typeName, ReplyTo = replyTo, CorrelationId = correlationId }, Encoding.UTF8.GetBytes(json));

    [Fact]
    public async Task Consumer_decodes_utf8_body_dispatches_command_and_acks()
    {
        var (probe, received) = await StartConsumer();

        await Deliver(probe, 5, TypeName<RabbitPing>(), JsonConvert.SerializeObject(new RabbitPing("c-1", Unicode)));

        var command = Assert.IsType<RabbitPing>(Assert.Single(received.Messages));
        Assert.Equal(("c-1", Unicode), (command.CommandId, command.Text));
        Assert.Equal(new[] { (5ul, false) }, probe.Acks);
        Assert.Empty(probe.Rejects);
        Assert.Empty(probe.Published);
    }

    [Fact]
    public async Task Consumer_dispatches_event_and_raw_message()
    {
        var (probe, received) = await StartConsumer();

        await Deliver(probe, 1, TypeName<RabbitHappened>(), "{\"EventId\":\"e-1\",\"What\":\"" + Unicode + "\"}");
        await Deliver(probe, 2, "Unknown.Type, Unknown", "{\"raw\":\"" + Unicode + "\"}");

        Assert.Equal(Unicode, Assert.IsType<RabbitHappened>(received.Messages[0]).What);
        Assert.Equal("{\"raw\":\"" + Unicode + "\"}", received.Messages[1]);
        Assert.Equal(new[] { (1ul, false), (2ul, false) }, probe.Acks);
    }

    [Fact]
    public async Task Consumer_answers_query_on_reply_queue_after_ack()
    {
        var (probe, received) = await StartConsumer();

        await Deliver(probe, 3, TypeName<RabbitLookup>(), "{\"QueryId\":\"q-1\",\"Term\":\"x\"}", replyTo: "reply-here", correlationId: "corr-9");

        Assert.IsType<RabbitLookup>(Assert.Single(received.Messages));
        Assert.Equal(new[] { (3ul, false) }, probe.Acks);
        var reply = Assert.Single(probe.Published);
        Assert.Equal(("", "reply-here", false), (reply.exchange, reply.routingKey, reply.mandatory));
        Assert.Equal("corr-9", reply.properties.CorrelationId);
        Assert.Equal(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new List<string> { "first", Unicode }, Formatting.None)), reply.body);
    }

    [Fact]
    public async Task Consumer_does_not_reply_without_reply_to()
    {
        var (probe, _) = await StartConsumer();

        await Deliver(probe, 4, TypeName<RabbitLookup>(), "{\"QueryId\":\"q-1\",\"Term\":\"x\"}");

        Assert.Equal(new[] { (4ul, false) }, probe.Acks);
        Assert.Empty(probe.Published);
    }

    [Fact]
    public async Task Consumer_rejects_without_requeue_when_handling_fails()
    {
        var (probe, received) = await StartConsumer();
        received.Throw = new InvalidOperationException("handler failed");

        await Deliver(probe, 8, TypeName<RabbitPing>(), "{\"CommandId\":\"c-1\",\"Text\":\"t\"}");

        Assert.Empty(probe.Acks);
        Assert.Equal(new[] { (8ul, false) }, probe.Rejects);
    }

    [Fact]
    public async Task Consumer_decodes_malformed_utf8_exactly_like_decoding_a_copied_array()
    {
        var (probe, received) = await StartConsumer();
        var random = new Random(17102026);
        var samples = new List<byte[]> {
            new byte[] { 0xC0, 0x80 }, new byte[] { 0xED, 0xA0, 0x80 }, new byte[] { 0xF4, 0x90, 0x80, 0x80 }, new byte[] { 0xE2, 0x82 },
            new byte[] { 0x80, 0xBF, 0xFE, 0xFF }, new byte[] { 0xEF, 0xBB, 0xBF }, new byte[] { 0xEF, 0xBF, 0xBE }, Encoding.UTF8.GetBytes(Unicode)
        };
        for (var i = 0; i < 500; i++) {
            // Random bytes that keep the JSON string literal valid: no quote, backslash or control characters.
            samples.Add(Enumerable.Range(0, random.Next(1, 48)).Select(_ => (byte)random.Next(0x20, 0x100)).Where(b => b != 0x22 && b != 0x5C).ToArray());
        }

        ulong tag = 0;
        foreach (var sample in samples) {
            var body = Encoding.ASCII.GetBytes("{\"raw\":\"").Concat(sample).Concat(Encoding.ASCII.GetBytes("\"}")).ToArray();
            received.Messages.Clear();

            await probe.Consumer.HandleBasicDeliverAsync("consumer-tag", ++tag, false, "exchange", "orders", new BasicProperties { Type = "Unknown.Type, Unknown" }, body);

            Assert.Equal(Encoding.UTF8.GetString(body.ToArray()), Assert.Single(received.Messages));
        }

        Assert.Equal(samples.Count, probe.Acks.Count);
        Assert.Empty(probe.Rejects);
    }

    [Fact]
    public async Task Parallel_queries_on_one_sender_each_get_their_own_reply()
    {
        var channel = new Mock<IChannel>();
        var consumers = new System.Collections.Concurrent.ConcurrentDictionary<string, IAsyncBasicConsumer>();
        var queues = 0;
        var deleted = new System.Collections.Concurrent.ConcurrentBag<string>();
        channel.Setup(value => value.QueueDeclareAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<IDictionary<string, object>>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult(new QueueDeclareOk($"reply-{Interlocked.Increment(ref queues)}", 0, 0)));
        channel.Setup(value => value.BasicConsumeAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<IDictionary<string, object>>(), It.IsAny<IAsyncBasicConsumer>(), It.IsAny<CancellationToken>()))
            .Callback<string, bool, string, bool, bool, IDictionary<string, object>, IAsyncBasicConsumer, CancellationToken>((queue, _, _, _, _, _, consumer, _) => consumers[queue] = consumer)
            .ReturnsAsync("ctag");
        channel.Setup(value => value.QueueDeleteAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Callback<string, bool, bool, bool, CancellationToken>((queue, _, _, _, _) => deleted.Add(queue))
            .ReturnsAsync(0u);
        channel.Setup(value => value.BasicPublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<BasicProperties>(), It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, bool, BasicProperties, ReadOnlyMemory<byte>, CancellationToken>((_, _, _, properties, body, _) => {
                var term = Newtonsoft.Json.Linq.JObject.Parse(Encoding.UTF8.GetString(body.Span))["Term"]!.ToString();
                var replyTo = properties.ReplyTo;
                var correlationId = properties.CorrelationId;
                var delay = (term.GetHashCode() & 0x7fffffff) % 20;
                _ = Task.Run(async () => {
                    await Task.Delay(delay);
                    var consumer = consumers[replyTo];
                    await consumer.HandleBasicDeliverAsync("ctag", 1, false, "", replyTo, new BasicProperties { CorrelationId = "someone-else" }, Encoding.UTF8.GetBytes("[\"wrong\"]"));
                    await consumer.HandleBasicDeliverAsync("ctag", 2, false, "", replyTo, new BasicProperties { CorrelationId = correlationId }, Encoding.UTF8.GetBytes($"[\"{term}\"]"));
                });
            })
            .Returns(ValueTask.CompletedTask);
        var sender = new RabbitQuerySender(new LogCapture(LogEventLevel.Information).Logger, channel.Object, "orders", "");

        var results = await Task.WhenAll(Enumerable.Range(0, 50).Select(i =>
            sender.SendAsync(new RabbitLookup($"q-{i}", $"term-{i}"), new BasicProperties { Headers = new Dictionary<string, object>() })));

        for (var i = 0; i < 50; i++) {
            Assert.Equal(new List<string> { $"term-{i}" }, results[i]);
        }
        Assert.Equal(50, deleted.Distinct().Count());
    }

    [Fact]
    public async Task Consumer_handles_empty_body_as_empty_object()
    {
        var (probe, received) = await StartConsumer();

        await Deliver(probe, 6, TypeName<RabbitPing>(), "");

        Assert.Null(Assert.IsType<RabbitPing>(Assert.Single(received.Messages)).Text);
        Assert.Equal(new[] { (6ul, false) }, probe.Acks);
    }

    // --- RabbitQuerySender ---

    [Fact]
    public async Task Query_sender_publishes_to_default_exchange_and_returns_matching_reply()
    {
        var probe = new ChannelProbe();
        probe.AfterPublish = properties => _ = Task.Run(async () => {
            await Task.Delay(50);
            await probe.Reply("other-correlation", "[\"wrong\"]");
            await probe.Reply(properties.CorrelationId, "[\"a\",\"" + Unicode + "\"]");
            await probe.Reply(properties.CorrelationId, "[\"late duplicate\"]");
        });
        var sender = new RabbitQuerySender(new LogCapture(LogEventLevel.Information).Logger, probe.Channel.Object, "orders", "ignored-routing-key");
        var properties = new BasicProperties { Headers = new Dictionary<string, object>() };

        var result = await sender.SendAsync(new RabbitLookup("q-1", "term"), properties);

        Assert.Equal(new List<string> { "a", Unicode }, result);
        var request = Assert.Single(probe.Published);
        Assert.Equal(("", "orders", false), (request.exchange, request.routingKey, request.mandatory));
        Assert.Equal("amq.gen-reply", request.properties.ReplyTo);
        Assert.Equal(TypeName<RabbitLookup>(), request.properties.Type);
        Assert.True(Guid.TryParse(request.properties.CorrelationId, out _));
        Assert.Equal(JsonConvert.SerializeObject(new RabbitLookup("q-1", "term"), Formatting.None), Encoding.UTF8.GetString(request.body));
        Assert.Equal(new[] { "amq.gen-reply" }, probe.DeletedQueues);
    }

    [Fact]
    public async Task Query_sender_keeps_existing_correlation_id_and_accepts_reply_before_waiting()
    {
        var probe = new ChannelProbe();
        probe.AfterPublish = properties => probe.Reply(properties.CorrelationId, "[\"sync\"]").GetAwaiter().GetResult();
        var sender = new RabbitQuerySender(new LogCapture(LogEventLevel.Information).Logger, probe.Channel.Object, "orders", "");
        var properties = new BasicProperties { CorrelationId = "preset", Headers = new Dictionary<string, object>() };

        var result = await sender.SendAsync(new RabbitLookup("q-1", "term"), properties);

        Assert.Equal(new List<string> { "sync" }, result);
        Assert.Equal("preset", Assert.Single(probe.Published).properties.CorrelationId);
        Assert.Equal(new[] { "amq.gen-reply" }, probe.DeletedQueues);
    }

    [Fact]
    public async Task Query_sender_cancellation_while_waiting_throws_operation_canceled_and_keeps_queue()
    {
        var probe = new ChannelProbe();
        var sender = new RabbitQuerySender(new LogCapture(LogEventLevel.Information).Logger, probe.Channel.Object, "orders", "");
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        var exception = await Assert.ThrowsAsync<OperationCanceledException>(() =>
            sender.SendAsync(new RabbitLookup("q-1", "term"), new BasicProperties { Headers = new Dictionary<string, object>() }, cancellation.Token));

        Assert.Equal(cancellation.Token, exception.CancellationToken);
        Assert.Empty(probe.DeletedQueues);
    }

    [Fact]
    public async Task Query_sender_with_already_canceled_token_throws_even_if_reply_is_there()
    {
        var probe = new ChannelProbe();
        probe.AfterPublish = properties => probe.Reply(properties.CorrelationId, "[\"sync\"]").GetAwaiter().GetResult();
        var sender = new RabbitQuerySender(new LogCapture(LogEventLevel.Information).Logger, probe.Channel.Object, "orders", "");
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var exception = await Assert.ThrowsAsync<OperationCanceledException>(() =>
            sender.SendAsync(new RabbitLookup("q-1", "term"), new BasicProperties { Headers = new Dictionary<string, object>() }, cancellation.Token));

        Assert.Equal(cancellation.Token, exception.CancellationToken);
        Assert.Empty(probe.DeletedQueues);
    }

    [Fact]
    public async Task Query_sender_logs_the_query_at_verbose_level()
    {
        var probe = new ChannelProbe();
        probe.AfterPublish = properties => probe.Reply(properties.CorrelationId, "[]").GetAwaiter().GetResult();
        var verbose = new LogCapture(LogEventLevel.Verbose);
        var information = new LogCapture(LogEventLevel.Information);

        await new RabbitQuerySender(verbose.Logger, probe.Channel.Object, "orders", "rk").SendAsync(new RabbitLookup("q-1", "term"), new BasicProperties { Headers = new Dictionary<string, object>() });
        await new RabbitQuerySender(information.Logger, probe.Channel.Object, "orders", "rk").SendAsync(new RabbitLookup("q-1", "term"), new BasicProperties { Headers = new Dictionary<string, object>() });

        Assert.Equal(new[] {
            "Verbose | Sending {queryType} using RabbitMQ sender to exchange '{exchange}', routing-key: '{routingKey}' | exchange=\"orders\"; query=RabbitLookup { QueryId: \"q-1\", Term: \"term\" }; queryType=\"RabbitLookup\"; routingKey=\"rk\""
        }, verbose.Lines);
        Assert.Empty(information.Lines);
    }

    // --- RabbitCommandSender / RabbitEventPublisher ---

    [Theory]
    [InlineData(LogEventLevel.Verbose, true)]
    [InlineData(LogEventLevel.Debug, false)]
    public async Task Command_sender_publishes_json_with_type_name(LogEventLevel level, bool logged)
    {
        var probe = new ChannelProbe();
        var capture = new LogCapture(level);
        var sender = new RabbitCommandSender(capture.Logger, probe.Channel.Object, "orders-exchange", "orders-key");
        var command = new RabbitPing("c-7", Unicode);

        await sender.SendAsync(command, new BasicProperties { Headers = new Dictionary<string, object>() }, CancellationToken.None);

        var published = Assert.Single(probe.Published);
        Assert.Equal(("orders-exchange", "orders-key", false), (published.exchange, published.routingKey, published.mandatory));
        Assert.Equal(TypeName<RabbitPing>(), published.properties.Type);
        Assert.Equal(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(command, Formatting.None)), published.body);
        Assert.Equal(logged
            ? new[] { $"Verbose | Sending {{commandType}} using RabbitMQ sender to exchange '{{exchange}}', routing-key: '{{routingKey}}' | command=RabbitPing {{ CommandId: \"c-7\", Text: \"{Unicode}\" }}; commandType=\"RabbitPing\"; exchange=\"orders-exchange\"; routingKey=\"orders-key\"" }
            : Array.Empty<string>(), capture.Lines);
    }

    [Theory]
    [InlineData(LogEventLevel.Verbose, true)]
    [InlineData(LogEventLevel.Information, false)]
    public async Task Event_publisher_unwraps_routable_events(LogEventLevel level, bool logged)
    {
        var probe = new ChannelProbe();
        var capture = new LogCapture(level);
        var publisher = new RabbitEventPublisher(capture.Logger, probe.Channel.Object, "events", "default-key");
        var inner = new RabbitHappened("e-1", "moved");

        await publisher.PublishAsync(new RoutableEvent(inner, "special-key"), new BasicProperties { Headers = new Dictionary<string, object>() });
        await publisher.PublishAsync(inner, new BasicProperties { Headers = new Dictionary<string, object>() });

        Assert.Equal(2, probe.Published.Count);
        Assert.Equal(("events", "special-key", false), (probe.Published[0].exchange, probe.Published[0].routingKey, probe.Published[0].mandatory));
        Assert.Equal(("events", "default-key"), (probe.Published[1].exchange, probe.Published[1].routingKey));
        foreach (var published in probe.Published) {
            Assert.Equal("e-1", published.properties.MessageId);
            Assert.Equal(TypeName<RabbitHappened>(), published.properties.Type);
            Assert.Equal(JsonConvert.SerializeObject(inner, Formatting.None), Encoding.UTF8.GetString(published.body));
        }
        Assert.Equal(logged
            ? new[] {
                "Verbose | Publishing {eventType} using RabbitMQ publisher to exchange '{exchange}', routing-key: '{routingKey}' | event=RabbitHappened { EventId: \"e-1\", What: \"moved\" }; eventType=\"RoutableEvent\"; exchange=\"events\"; routingKey=\"special-key\"",
                "Verbose | Publishing {eventType} using RabbitMQ publisher to exchange '{exchange}', routing-key: '{routingKey}' | event=RabbitHappened { EventId: \"e-1\", What: \"moved\" }; eventType=\"RabbitHappened\"; exchange=\"events\"; routingKey=\"default-key\"",
            }
            : Array.Empty<string>(), capture.Lines);
    }
}
