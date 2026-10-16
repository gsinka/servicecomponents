using System;
using System.Threading.Tasks;
using ServiceComponents.Api.Contexts;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Infrastructure.Behaviors.ProcessContextBehavior;
using Xunit;

namespace ServiceComponents.Test
{
    public class ProcessContextBehaviorTests
    {
        private const string TitleKey = "TITLE";
        private const string CodeKey = "CODE";

        [ProcessContext("Order", "ORDER")]
        private class OuterCommand : ICommand
        {
            public string CommandId => "outer";
        }

        [ProcessContext("Payment", "PAYMENT")]
        private class InnerCommand : ICommand
        {
            public string CommandId => "inner";
        }

        [ProcessContext("Title only")]
        private class TitleOnlyCommand : ICommand
        {
            public string CommandId => "title-only";
        }

        private class PlainCommand : ICommand
        {
            public string CommandId => "plain";
        }

        [ProcessContext("Query", "QUERY")]
        private class AttributedQuery : IQuery
        {
            public string QueryId => "query";
        }

        private static ProcessContextBehavior CreateBehavior() =>
            new(new ProcessContextBehaviorOptions(TitleKey, CodeKey));

        [Fact]
        public async Task SetsTitleAndContextCode()
        {
            var exception = new InvalidOperationException();

            await CreateBehavior().HandleFailureAsync(new InnerCommand(), exception);

            Assert.Equal("Payment", exception.Data[TitleKey]);
            Assert.Equal("PAYMENT", exception.Data[CodeKey]);
        }

        [Fact]
        public async Task SetsOnlyTitleWhenNoContextCode()
        {
            var exception = new InvalidOperationException();

            await CreateBehavior().HandleFailureAsync(new TitleOnlyCommand(), exception);

            Assert.Equal("Title only", exception.Data[TitleKey]);
            Assert.False(exception.Data.Contains(CodeKey));
        }

        [Fact]
        public async Task DoesNothingWithoutAttribute()
        {
            var exception = new InvalidOperationException();

            await CreateBehavior().HandleFailureAsync(new PlainCommand(), exception);

            Assert.Empty(exception.Data);
        }

        [Fact]
        public async Task SameExceptionPassingTwiceKeepsInnermostContext()
        {
            var behavior = CreateBehavior();
            var exception = new InvalidOperationException();

            // inner dispatch fails first, then the outer dispatch sees the same instance
            await behavior.HandleFailureAsync(new InnerCommand(), exception);
            await behavior.HandleFailureAsync(new OuterCommand(), exception);

            Assert.Equal("Payment", exception.Data[TitleKey]);
            Assert.Equal("PAYMENT", exception.Data[CodeKey]);
        }

        [Fact]
        public async Task SameExceptionPassingTwiceDoesNotThrowForQueries()
        {
            var behavior = CreateBehavior();
            var exception = new InvalidOperationException();

            await behavior.HandleFailureAsync(new AttributedQuery(), exception);
            var ex = await Record.ExceptionAsync(() => behavior.HandleFailureAsync(new AttributedQuery(), exception));

            Assert.Null(ex);
            Assert.Equal("Query", exception.Data[TitleKey]);
        }
    }
}
