namespace JustSaying.AsyncApi;

/// <summary>
/// Contact information for the API, written to the AsyncAPI document's <c>info.contact</c>.
/// </summary>
public sealed class AsyncApiContactOptions
{
    /// <summary>
    /// Gets or sets the identifying name of the contact person or organization.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the URL pointing to the contact information.
    /// </summary>
    public Uri Url { get; set; }

    /// <summary>
    /// Gets or sets the email address of the contact person or organization.
    /// </summary>
    public string Email { get; set; }
}
