using Autofac;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using ServiceComponents.Application.Senders;
using ServiceComponents.Testing;
using ServiceComponents.Testing.Extensions;
using WebApplication1;

namespace ReferenceApplication.Test
{
    public class SampleTestHost2 : TestHost<Program>
    {
        public override void ConfigureTestContainer(HostBuilderContext context, ContainerBuilder container)
        {
        }

        public override void ConfigureTestServices(HostBuilderContext context, IServiceCollection services)
        {
        }
    }

    public class SampleTestHost : TestHost<Program>
    {
        public Mock<IPublishEvent> EventPublisherMock = new();

        public override void ConfigureTestContainer(HostBuilderContext context, ContainerBuilder container)
        {
            container.UseTestPublisher(EventPublisherMock.Object);
        }

        public override void ConfigureTestServices(HostBuilderContext context, IServiceCollection services)
        {

        }
    }
}
