namespace JustSaying.AsyncApi;

/// <summary>
/// A tag used to group the application logically, written to the AsyncAPI document's <c>info.tags</c>.
/// </summary>
public sealed class AsyncApiTagOptions
{
    /// <summary>
    /// Gets or sets the name of the tag. Required for the tag to be written.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets a description of the tag.
    /// </summary>
    public string Description { get; set; }
}
