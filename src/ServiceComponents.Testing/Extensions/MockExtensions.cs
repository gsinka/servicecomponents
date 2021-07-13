using System;
using System.Threading;
using Moq;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Application.Senders;

namespace ServiceComponents.Testing.Extensions
{
    public static class MockExtensions
    {
        public static void Published<TEvent>(this Mock<IPublishEvent> mock, int count = 1)
            where TEvent : IEvent
        {
            mock.Verify(m => m.PublishAsync(It.IsAny<TEvent>(), It.IsAny<CancellationToken>()), Times.Exactly(count));
        }

        public static void Published<TEvent>(this Mock<IPublishEvent> mock, Func<TEvent, bool> validate, int count = 1)
            where TEvent : IEvent
        {
            mock.Verify(m => m.PublishAsync(It.Is<TEvent>(e => validate(e)), It.IsAny<CancellationToken>()), Times.Exactly(count));
        }
    }
}
