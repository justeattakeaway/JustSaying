using System;
using System.Threading.Tasks;
using JustSaying.Messaging;
using JustSaying.TestingFramework;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit.Abstractions;

namespace JustSaying.IntegrationTests.Fluent.AwsTools
{
    public class WhenUsingServerSideEncryption : IntegrationTestBase
    {
        public WhenUsingServerSideEncryption(ITestOutputHelper outputHelper)
            : base(outputHelper)
        {
        }

        [AwsFact]
        public async Task Then_The_Message_Is_Handled()
        {
            // Arrange
            var completionSource = new TaskCompletionSource<object>();
            var handler = CreateHandler<SimpleMessage>(completionSource);

            string masterKeyId = "alias/aws/sqs";

            var services = GivenJustSaying()
                .ConfigureJustSaying(
                    (builder) => builder.Publications((options) => options.WithQueue<SimpleMessage>(
                        (queue) => queue.WithWriteConfiguration(
                            (config) => config.WithQueueName(UniqueName).WithEncryption(masterKeyId)))))
                .ConfigureJustSaying(
                    (builder) => builder.Subscriptions((options) => options.ForQueue<SimpleMessage>(
                        (queue) => queue.WithName(UniqueName).WithReadConfiguration(
                            (config) => config.WithEncryption(masterKeyId)))))
                .AddSingleton(handler);

            string content = Guid.NewGuid().ToString();

            var message = new SimpleMessage()
            {
                Content = content
            };

            await WhenAsync(
                services,
                async (publisher, listener, cancellationToken) =>
                {
                    listener.Start(cancellationToken);

                    // Act
                    await publisher.PublishAsync(message, cancellationToken);

                    // Assert
                    completionSource.Task.Wait(cancellationToken);

                    await handler.Received().Handle(Arg.Is<SimpleMessage>((m) => m.Content == content));
                });
        }
    }
}
