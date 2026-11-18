namespace JustSaying.AsyncApi;

/// <summary>
/// External documentation for the application, written to the AsyncAPI document's <c>info.externalDocs</c>.
/// </summary>
public sealed class AsyncApiExternalDocsOptions
{
    /// <summary>
    /// Gets or sets the URL of the documentation. Required for the documentation to be written.
    /// </summary>
    public Uri Url { get; set; }

    /// <summary>
    /// Gets or sets a description of the documentation.
    /// </summary>
    public string Description { get; set; }
}
