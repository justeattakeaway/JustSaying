namespace JustSaying.Fluent
{
    /// <summary>
    /// Defines a builder for a publication.
    /// </summary>
    internal interface IPublicationBuilder
    {
        /// <summary>
        /// Configures the publication for the <see cref="JustSayingFluently"/>.
        /// </summary>
        void Configure(JustSayingFluently bus);
    }
}
