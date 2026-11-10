namespace JustSaying.AsyncApi;

/// <summary>
/// License information for the API, written to the AsyncAPI document's <c>info.license</c>.
/// </summary>
public sealed class AsyncApiLicenseOptions
{
    /// <summary>
    /// Gets or sets the name of the license. Required for the license to be written.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the URL of the license.
    /// </summary>
    public Uri Url { get; set; }
}
