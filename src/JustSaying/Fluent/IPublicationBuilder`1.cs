using JustSaying.AwsTools;
using JustSaying.Models;
using Microsoft.Extensions.Logging;

namespace JustSaying.Fluent;

/// <summary>
/// Defines a builder for a publication.
/// </summary>
/// <typeparam name="T">
/// The type of the messages to publish.
/// </typeparam>
internal interface IPublicationBuilder<out T>
    where T : class
{
    /// <summary>
    /// Configures the publication for the <see cref="JustSayingBus"/>.
    /// </summary>
    /// <param name="bus">The <see cref="JustSayingBus"/> to configure subscriptions for.</param>
    /// <param name="proxy">The <see cref="IAwsClientFactoryProxy"/> to use to create SQS/SNS clients with.</param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/> logger factory to use.</param>
    void Configure(JustSayingBus bus, IAwsClientFactoryProxy proxy, ILoggerFactory loggerFactory);
}
