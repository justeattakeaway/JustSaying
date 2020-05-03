namespace JustSaying.Fluent
{
    /// <summary>
    /// Defines a builder for a subscription.
    /// </summary>
    internal interface ISubscriptionBuilder
    {
        /// <summary>
        /// Configures the subscription for the <see cref="JustSayingFluently"/>.
        /// </summary>
        /// <param name="bus">The <see cref="JustSayingFluently"/> to configure the subscription for.</param>
        void Configure(JustSayingFluently bus, IHandlerResolver resolver);
    }
}
