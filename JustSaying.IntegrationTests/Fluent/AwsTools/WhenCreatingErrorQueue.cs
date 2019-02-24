using System;
using System.Threading.Tasks;
using JustSaying.AwsTools;
using JustSaying.AwsTools.QueueCreation;
using Microsoft.Extensions.Logging;
using Shouldly;
using Xunit.Abstractions;

namespace JustSaying.IntegrationTests.Fluent.AwsTools
{
    public class WhenCreatingErrorQueue : IntegrationTestBase
    {
        public WhenCreatingErrorQueue(ITestOutputHelper outputHelper)
            : base(outputHelper)
        {
        }

        [AwsFact]
        public async Task Then_The_Retention_Period_Stays_At_Maximum()
        {
            // Arrange
            ILoggerFactory loggerFactory = OutputHelper.ToLoggerFactory();
            IAwsClientFactory clientFactory = CreateClientFactory();

            var client = clientFactory.GetSqsClient(Region);

            var queue = new ErrorQueue(
                Region,
                UniqueName,
                client,
                loggerFactory);

            var queueConfig = new SqsBasicConfiguration()
            {
                ErrorQueueRetentionPeriod = JustSayingConstants.MaximumRetentionPeriod,
                ErrorQueueOptOut = true,
            };

            // Act
            await queue.CreateAsync(queueConfig);

            queueConfig.ErrorQueueRetentionPeriod = TimeSpan.FromSeconds(100);

            await queue.UpdateQueueAttributeAsync(queueConfig);

            // Assert
            queue.MessageRetentionPeriod.ShouldBe(TimeSpan.FromSeconds(100));
        }
    }
}
