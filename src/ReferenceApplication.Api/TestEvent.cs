using ServiceComponents.Api.Mediator;

namespace ReferenceApplication.Api
{
    public class TestEvent : Event
    {
        public string Data { get; }

        public TestEvent(string data)
        {
            Data = data;
        }
    }
}