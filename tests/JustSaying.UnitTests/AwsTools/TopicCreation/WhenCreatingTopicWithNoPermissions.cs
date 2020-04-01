using System;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.MessageSerialization;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

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
        public async Task Cannot_Create_Topic_Because_It_Exists()
        {
            // Arrange
            string topicName = Guid.NewGuid().ToString();
            ILoggerFactory loggerFactory = OutputHelper.ToLoggerFactory();

            var subjectProvider = new NonGenericMessageSubjectProvider();
            var serializerFactor = new NewtonsoftSerializationFactory();
            var serializationRegister = new MessageSerializationRegister(subjectProvider, serializerFactor);

            IAmazonSimpleNotificationService client = CreateSnsClient(exists: true);

            var topic = new SnsTopicByName(
                topicName,
                client,
                serializationRegister,
                loggerFactory,
                subjectProvider);

            // Act
            bool actual = await topic.CreateAsync();

            // Assert
            actual.ShouldBeFalse();
            topic.Arn.ShouldNotBeNull();
        }

        [Fact]
        public async Task Cannot_Create_Topic_Because_Not_Authorized()
        {
            // Arrange
            string topicName = Guid.NewGuid().ToString();
            ILoggerFactory loggerFactory = OutputHelper.ToLoggerFactory();

            var subjectProvider = new NonGenericMessageSubjectProvider();
            var serializerFactor = new NewtonsoftSerializationFactory();
            var serializationRegister = new MessageSerializationRegister(subjectProvider, serializerFactor);

            IAmazonSimpleNotificationService client = CreateSnsClient(exists: false);

            var topic = new SnsTopicByName(
                topicName,
                client,
                serializationRegister,
                loggerFactory,
                subjectProvider);

            // Act and Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => topic.CreateAsync());
        }

        private IAmazonSimpleNotificationService CreateSnsClient(bool exists)
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
