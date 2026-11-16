using JustSaying.Fluent;
using JustSaying.Messaging;
using JustSaying.TestingFramework;
using Microsoft.Extensions.DependencyInjection;

namespace JustSaying.IntegrationTests.Fluent.Publishing;

public class WhenATopicIsNamedTwice : IntegrationTestBase
{
    [Test]
    public void Then_A_Destination_Name_And_A_Topic_Name_Function_Are_Rejected()
    {
        // Arrange
        var serviceProvider = GivenJustSaying()
            .ConfigureJustSaying((builder) => builder.Publications((options) =>
                options.WithTopic<SimpleMessage>(TopicDestination.Named("orders"), c => c.WithTopicName(m => "orders-dynamic"))))
            .BuildServiceProvider();

        // Act and Assert
        Should.Throw<InvalidOperationException>(() => serviceProvider.GetRequiredService<IMessagePublisher>())
            .Message.ShouldBe("The topic is named both by the TopicDestination destination ('orders') and a WithTopicName function that names it per message; name it once.");
    }

    [Test]
    public void Then_A_Static_Topic_Name_And_A_Topic_Name_Function_Are_Rejected()
    {
        // Arrange
        var serviceProvider = GivenJustSaying()
            .ConfigureJustSaying((builder) => builder.Publications((options) =>
                options.WithTopic<SimpleMessage>(c => c.WithTopicName("orders").WithTopicName(m => "orders-dynamic"))))
            .BuildServiceProvider();

        // Act and Assert
        Should.Throw<InvalidOperationException>(() => serviceProvider.GetRequiredService<IMessagePublisher>())
            .Message.ShouldBe("The topic is named both by WithTopicName ('orders') and a WithTopicName function that names it per message; name it once.");
    }
}
