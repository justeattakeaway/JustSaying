using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using JustSaying.AwsTools.MessageHandling;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using Xunit;
using Xunit.Abstractions;
#pragma warning disable 618

namespace JustSaying.UnitTests.AwsTools.TopicCreation
{
    public class WhenCreatingTopicWithNoPermissions
    {
        public WhenCreatingTopicWithNoPermissions(ITestOutputHelper outputHelper)
        {
            OutputHelper = outputHelper;
        }

        private ITestOutputHelper OutputHelper { get; }

        [Fact]
        public async Task Arn_Still_Retrieved_When_It_Already_Exists()
        {
            // Arrange
            string topicName = Guid.NewGuid().ToString();
            ILoggerFactory loggerFactory = OutputHelper.ToLoggerFactory();

            IAmazonSimpleNotificationService client = CreateSnsClient(exists: true);

            var topic = new SnsTopicByName(
                topicName,
                client,
                loggerFactory);

            // Act
            await topic.CreateAsync(CancellationToken.None);

            // Assert
            topic.Arn.ShouldNotBeNull();
        }

        [Fact]
        public async Task Cannot_Create_Topic_Because_Not_Authorized()
        {
            // Arrange
            string topicName = Guid.NewGuid().ToString();
            ILoggerFactory loggerFactory = OutputHelper.ToLoggerFactory();

            IAmazonSimpleNotificationService client = CreateSnsClient(exists: false);

            var topic = new SnsTopicByName(
                topicName,
                client,
                loggerFactory);

            // Act and Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => topic.CreateAsync(CancellationToken.None));
        }

        private static IAmazonSimpleNotificationService CreateSnsClient(bool exists)
        {
            var client = Substitute.For<IAmazonSimpleNotificationService>();

            client.CreateTopicAsync(Arg.Any<CreateTopicRequest>())
                  .ThrowsForAnyArgs((_) => new AuthorizationErrorException("Denied"));

            if (exists)
            {
                client.FindTopicAsync(Arg.Any<string>())
                      .ReturnsForAnyArgs(Task.FromResult(new Topic() { TopicArn = "foo" }));
            }
            else
            {
                client.FindTopicAsync(Arg.Any<string>())
                      .ReturnsForAnyArgs(Task.FromResult<Topic>(null));
            }

            return client;
        }
    }
}
