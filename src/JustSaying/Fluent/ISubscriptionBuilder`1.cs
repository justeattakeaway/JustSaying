using System.Threading.Tasks;
using JustSaying.AwsTools;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Models;
using Microsoft.Extensions.Logging;

namespace JustSaying.Fluent
{
    /// <summary>
    /// Defines a builder for a subscription.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the messages to subscribe to.
    /// </typeparam>
    internal interface ISubscriptionBuilder<out T>
        where T : Message
    {
        /// <summary>
        /// Configures the subscription for the <see cref="JustSayingFluently"/>.
        /// </summary>
        /// <param name="bus">The <see cref="JustSayingFluently"/> to configure the subscription for.</param>
        /// <param name="resolver"></param>
        /// <param name="creator"></param>
        /// <param name="loggerFactory"></param>
        /// <param name="proxy"></param>
        Task ConfigureAsync(
            JustSayingBus bus,
            IHandlerResolver resolver,
            IVerifyAmazonQueues creator,
            ILoggerFactory loggerFactory);
    }
}
