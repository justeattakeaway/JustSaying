namespace JustSaying.Fluent
{
    /// <summary>
    /// Defines a builder that is owned by another builder.
    /// </summary>
    /// <typeparam name="TChild">The type of the child builder.</typeparam>
    /// <typeparam name="TParent">The type of the parent builder.</typeparam>
    public interface IBuilderChild<TChild, TParent> : IBuilder<TChild>
        where TChild : class
        where TParent : class
    {
        /// <summary>
        /// Gets the parent of this builder.
        /// </summary>
        TParent Parent { get; }
    }
}
