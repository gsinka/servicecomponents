using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Serilog.Events;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Infrastructure.Behaviors.Logging;
using Xunit;

namespace ServiceComponents.Test;

/// <summary>
/// Pins what LogBehavior writes (level, template, properties, destructured payloads) at each minimum level.
/// These tests describe the behavior before the performance work and must keep passing after it.
/// </summary>
public class LogBehaviorCharacterizationTests
{
    public class OrderCommand : ICommand
    {
        public string CommandId { get; }
        public string Product { get; }
        public int Quantity { get; }

        public OrderCommand(string commandId, string product, int quantity)
        {
            CommandId = commandId;
            Product = product;
            Quantity = quantity;
        }
    }

    public class OrderQuery : IQuery<List<OrderRow>>
    {
        public string QueryId { get; }
        public string Customer { get; }

        public OrderQuery(string queryId, string customer)
        {
            QueryId = queryId;
            Customer = customer;
        }
    }

    public class OrderRow
    {
        public string Product { get; }
        public int Quantity { get; }

        public OrderRow(string product, int quantity)
        {
            Product = product;
            Quantity = quantity;
        }
    }

    public class OrderEvent : IEvent
    {
        public string EventId { get; }
        public string Product { get; }

        public OrderEvent(string eventId, string product)
        {
            EventId = eventId;
            Product = product;
        }
    }

    private static async Task<IReadOnlyList<string>> RunEveryMethod(LogEventLevel level)
    {
        var capture = new LogCapture(level);
        var behavior = new LogBehavior(capture.Logger);

        var command = new OrderCommand("cmd-1", "apple", 3);
        var query = new OrderQuery("qry-1", "alice");
        var result = new List<OrderRow> { new("apple", 3), new("pear", 1) };
        var @event = new OrderEvent("evt-1", "apple");

        await behavior.PreHandleAsync(command);
        await behavior.PostHandleAsync(command);
        await behavior.HandleFailureAsync(command, new InvalidOperationException("command failed"));

        await behavior.PreHandleAsync(query);
        await behavior.PostHandleAsync(query, result);
        await behavior.HandleFailureAsync(query, new InvalidOperationException("query failed"));

        await behavior.PreHandleAsync(@event);
        await behavior.PostHandleAsync(@event);
        await behavior.HandleFailureAsync(@event, new InvalidOperationException("event failed"));

        return capture.Lines;
    }

    [Fact]
    public async Task Verbose_level_writes_every_message_with_destructured_payloads()
    {
        Assert.Equal(new[] {
            "Verbose | Handling {commandType} | commandData=OrderCommand { CommandId: \"cmd-1\", Product: \"apple\", Quantity: 3 }; commandType=\"OrderCommand\"",
            "Verbose | Handling {commandType} succeeded | commandType=\"OrderCommand\"",
            "Error | Handling {commandType} failed | commandType=\"OrderCommand\" | InvalidOperationException: command failed",
            "Verbose | Handling {queryType} | queryData=OrderQuery { QueryId: \"qry-1\", Customer: \"alice\" }; queryType=\"OrderQuery\"",
            "Verbose | Handling {queryType} succeeded | queryResult=[OrderRow { Product: \"apple\", Quantity: 3 }, OrderRow { Product: \"pear\", Quantity: 1 }]; queryType=\"OrderQuery\"",
            "Error | Handling {queryType} failed | queryType=\"OrderQuery\" | InvalidOperationException: query failed",
            "Debug | Handling {eventType} | eventData=OrderEvent { EventId: \"evt-1\", Product: \"apple\" }; eventType=\"OrderEvent\"",
            "Verbose | Handling {eventType} succeeded | eventType=\"OrderEvent\"",
            "Error | Handling {eventType} failed | eventType=\"OrderEvent\" | InvalidOperationException: event failed",
        }, await RunEveryMethod(LogEventLevel.Verbose));
    }

    [Fact]
    public async Task Debug_level_writes_only_event_start_and_failures()
    {
        Assert.Equal(new[] {
            "Error | Handling {commandType} failed | commandType=\"OrderCommand\" | InvalidOperationException: command failed",
            "Error | Handling {queryType} failed | queryType=\"OrderQuery\" | InvalidOperationException: query failed",
            "Debug | Handling {eventType} | eventData=OrderEvent { EventId: \"evt-1\", Product: \"apple\" }; eventType=\"OrderEvent\"",
            "Error | Handling {eventType} failed | eventType=\"OrderEvent\" | InvalidOperationException: event failed",
        }, await RunEveryMethod(LogEventLevel.Debug));
    }

    [Theory]
    [InlineData(LogEventLevel.Information)]
    [InlineData(LogEventLevel.Warning)]
    [InlineData(LogEventLevel.Error)]
    public async Task Information_and_above_writes_only_failures(LogEventLevel level)
    {
        Assert.Equal(new[] {
            "Error | Handling {commandType} failed | commandType=\"OrderCommand\" | InvalidOperationException: command failed",
            "Error | Handling {queryType} failed | queryType=\"OrderQuery\" | InvalidOperationException: query failed",
            "Error | Handling {eventType} failed | eventType=\"OrderEvent\" | InvalidOperationException: event failed",
        }, await RunEveryMethod(level));
    }

    [Fact]
    public async Task Fatal_level_writes_nothing()
    {
        Assert.Empty(await RunEveryMethod(LogEventLevel.Fatal));
    }

    [Fact]
    public async Task Every_method_returns_a_completed_task()
    {
        var behavior = new LogBehavior(new LogCapture(LogEventLevel.Information).Logger);
        var command = new OrderCommand("cmd-1", "apple", 3);

        Assert.True(behavior.PreHandleAsync(command).IsCompletedSuccessfully);
        Assert.True(behavior.PostHandleAsync(new OrderQuery("q", "c"), null).IsCompletedSuccessfully);
        Assert.True(behavior.PreHandleAsync(new OrderEvent("e", "p")).IsCompletedSuccessfully);
        await Task.CompletedTask;
    }
}
