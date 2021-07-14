using ServiceComponents.Testing;
using Xunit.Abstractions;

namespace ReferenceApplication.Test
{
    public class SampleTest : Test<SampleTestHost>
    {
        public SampleTest(SampleTestHost testHost, ITestOutputHelper outputHelper)
            : base(testHost, outputHelper)
        {
        }

        //[Fact(DisplayName = "Sample Http request")]
        //public async Task SampleHttpRequest()
        //{
        //    // Arrange
        //    var commandSender = TestHost.Services.GetRequiredService<ISendCommand>();

        //    // Act
        //    await commandSender.SendAsync(new TestCommand("fsdéflfásd"), cancellationToken: default);

        //    // Assert
        //    TestHost.EventPublisherMock.Published<TestEvent>();
        //}

        //[Fact(DisplayName = "Sample Http request 2")]
        //public async Task SampleHttpRequest2()
        //{
        //    // Arrange
        //    var client = TestHost.CreateClient();

        //    // Act
        //    var response = await client.GetAsync("/weatherforecast");

        //    // Assert
        //    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        //}
    }
}
