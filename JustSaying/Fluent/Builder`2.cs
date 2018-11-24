namespace JustSaying.Fluent
{
    /// <summary>
    /// The base class for implementations of <see cref="IBuilder{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type created by the builder.</typeparam>
    /// <typeparam name="TBuilder">The builder's own type.</typeparam>
    public abstract class Builder<T, TBuilder> : IBuilder<T>
        where T : class
        where TBuilder : Builder<T, TBuilder>
    {
        /// <summary>
        /// Gets the current <see cref="TBuilder"/>.
        /// </summary>
        public TBuilder And() => Self;

        /// <summary>
        /// Creates a new instance of <typeparamref name="T"/>.
        /// </summary>
        /// <returns>
        /// The created new instance of <see cref="T"/>.
        /// </returns>
        public abstract T Build();

        /// <summary>
        /// Gets the current <see cref="TBuilder"/>.
        /// </summary>
        protected abstract TBuilder Self { get; }
    }
}
