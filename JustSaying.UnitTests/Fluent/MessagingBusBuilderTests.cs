using System.Threading;
using Microsoft.Extensions.Logging;
using Xunit;

namespace JustSaying.Fluent
{
    public static class MessagingBusBuilderTests
    {
        [Fact]
        public static void Can_Create_Messaging_Bus_Fluently()
        {
            // Arrange
            var builder = new MessagingBusBuilder()
                .Client()
                    .WithBasicCredentials("accessKey", "secretKey")
                .And().Parent
                .Messaging()
                    .WithRegions("eu-west-1", "eu-central-1")
                    .And()
                    .WithActiveRegion("eu-west-1")
                .And().Parent
                .WithLoggerFactory(new LoggerFactory());

            // Assert
            IMessagingBus bus = builder.Build();

            bus.Start(new CancellationToken(canceled: true));
        }
    }
}
