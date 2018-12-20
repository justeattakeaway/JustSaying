namespace JustSaying.Fluent
{
    /// <summary>
    /// Defines a method for resolving instances of types from a dependency injection container.
    /// </summary>
    public interface IServiceResolver
    {
        /// <summary>
        /// Resolves an instance of the specified type.
        /// </summary>
        /// <typeparam name="T">
        /// The type to resolve an instance of.
        /// </typeparam>
        /// <returns>
        /// The resolved instance of <typeparamref name="T"/>.
        /// </returns>
        T ResolveService<T>();
    }
}
