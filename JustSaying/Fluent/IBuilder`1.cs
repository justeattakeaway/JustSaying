namespace JustSaying.Fluent
{
    /// <summary>
    /// Defines a method for creating instances of types with a builder pattern.
    /// </summary>
    /// <typeparam name="T">
    /// The type created by the builder.
    /// </typeparam>
    public interface IBuilder<T>
        where T : class
    {
        /// <summary>
        /// Creates a new instance of type <typeparamref name="T"/>.
        /// </summary>
        /// <returns>
        /// The created instance of <typeparamref name="T"/>.
        /// </returns>
        T Build();
    }
}
