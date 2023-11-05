using JustSaying.AwsTools;
using JustSaying.Models;
using Microsoft.Extensions.Logging;

namespace JustSaying.Fluent;

/// <summary>
/// Defines a builder for a publication.
/// </summary>
internal interface IPublicationBuilder
{
    /// <summary>
    /// Configures the publication for the <see cref="JustSayingBus"/>.
    /// </summary>
    /// <param name="bus">The <see cref="JustSayingBus"/> to configure subscriptions for.</param>
    /// <param name="proxy">The <see cref="IAwsClientFactoryProxy"/> to use to create SQS/SNS clients with.</param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/> logger factory to use.</param>
    void Configure(JustSayingBus bus, IAwsClientFactoryProxy proxy, ILoggerFactory loggerFactory);
}
