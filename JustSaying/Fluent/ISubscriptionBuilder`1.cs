using JustSaying.Models;

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
        void Configure(JustSayingFluently bus, IHandlerResolver resolver);
    }
}
