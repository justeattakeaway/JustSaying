using System.Threading.Tasks;
using JustSaying.AwsTools;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Models;
using Microsoft.Extensions.Logging;

namespace JustSaying.Fluent
{
    /// <summary>
    /// Defines a builder for a publication.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the messages to publish.
    /// </typeparam>
    internal interface IPublicationBuilder<out T>
        where T : Message
    {
        /// <summary>
        /// Configures the publication for the <see cref="JustSayingBus"/>.
        /// </summary>
        Task ConfigureAsync(JustSayingBus bus, IAwsClientFactoryProxy proxy, ILoggerFactory loggerFactory);
    }
}
