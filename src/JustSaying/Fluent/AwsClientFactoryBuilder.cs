using Amazon.Runtime;
using JustSaying.AwsTools;

namespace JustSaying.Fluent;

/// <summary>
/// A class representing a builder for instances of <see cref="IAwsClientFactory"/>. This class cannot be inherited.
/// </summary>
public sealed class AwsClientFactoryBuilder
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AwsClientFactoryBuilder"/> class.
    /// </summary>
    /// <param name="busBuilder">The <see cref="MessagingBusBuilder"/> that owns this instance.</param>
    internal AwsClientFactoryBuilder(MessagingBusBuilder busBuilder)
    {
        BusBuilder = busBuilder;
    }

    /// <summary>
    /// The <see cref="MessagingBusBuilder"/> that owns this instance.
    /// </summary>
    public MessagingBusBuilder BusBuilder { get; }

    /// <summary>
    /// Gets or sets a delegate to a method to create the <see cref="IAwsClientFactory"/> to use.
    /// </summary>
    private Func<IAwsClientFactory> ClientFactory { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="AWSCredentials"/> to use.
    /// </summary>
    private AWSCredentials Credentials { get; set; }

    /// <summary>
    /// Gets or sets the URI for the AWS services to use.
    /// </summary>
    private Uri ServiceUri { get; set; }

    /// <summary>
    /// Creates a new instance of <see cref="IAwsClientFactory"/>.
    /// </summary>
    /// <returns>
    /// The created instance of <see cref="IAwsClientFactory"/>.
    /// </returns>
    public IAwsClientFactory Build()
    {
        if (ClientFactory != null)
        {
            return ClientFactory();
        }

        DefaultAwsClientFactory factory;

        if (Credentials == null)
        {
            factory = new DefaultAwsClientFactory();
        }
        else
        {
            factory = new DefaultAwsClientFactory(Credentials);
        }

        factory.ServiceUri = ServiceUri;

        return factory;
    }

    /// <summary>
    /// Specifies the <see cref="IAwsClientFactory"/> to use.
    /// </summary>
    /// <param name="clientFactory">A delegate to a method to use to create an <see cref="IAwsClientFactory"/>.</param>
    /// <returns>
    /// The current <see cref="AwsClientFactoryBuilder"/>.
    /// </returns>
    public AwsClientFactoryBuilder WithClientFactory(Func<IAwsClientFactory> clientFactory)
    {
        ClientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
        return this;
    }

    /// <summary>
    /// Specifies the <see cref="AWSCredentials"/> to use.
    /// </summary>
    /// <param name="credentials">The <see cref="AWSCredentials"/> to use.</param>
    /// <returns>
    /// The current <see cref="AwsClientFactoryBuilder"/>.
    /// </returns>
    public AwsClientFactoryBuilder WithCredentials(AWSCredentials credentials)
    {
        Credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));
        return this;
    }

    /// <summary>
    /// Specifies that anonymous access to AWS should be used.
    /// </summary>
    /// <returns>
    /// The current <see cref="AwsClientFactoryBuilder"/>.
    /// </returns>
    public AwsClientFactoryBuilder WithAnonymousCredentials()
    {
        Credentials = new AnonymousAWSCredentials();
        return this;
    }

    /// <summary>
    /// Specifies the basic AWS credentials to use.
    /// </summary>
    /// <param name="accessKey">The access key to use.</param>
    /// <param name="secretKey">The secret key to use.</param>
    /// <returns>
    /// The current <see cref="AwsClientFactoryBuilder"/>.
    /// </returns>
    public AwsClientFactoryBuilder WithBasicCredentials(string accessKey, string secretKey)
    {
        Credentials = new BasicAWSCredentials(accessKey, secretKey);
        return this;
    }

    /// <summary>
    /// Specifies the basic AWS credentials to use.
    /// </summary>
    /// <param name="accessKeyId">The access key Id to use.</param>
    /// <param name="secretAccessKey">The secret access key to use.</param>
    /// <param name="token">The session token to use.</param>
    /// <returns>
    /// The current <see cref="AwsClientFactoryBuilder"/>.
    /// </returns>
    public AwsClientFactoryBuilder WithSessionCredentials(string accessKeyId, string secretAccessKey, string token)
    {
        Credentials = new SessionAWSCredentials(accessKeyId, secretAccessKey, token);
        return this;
    }

    /// <summary>
    /// Specifies the AWS service URL to use.
    /// </summary>
    /// <param name="url">The URL to use for AWS services.</param>
    /// <returns>
    /// The current <see cref="AwsClientFactoryBuilder"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="url"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="url"/> is not an absolute URI.
    /// </exception>
    public AwsClientFactoryBuilder WithServiceUrl(string url)
    {
        if (url == null)
        {
            throw new ArgumentNullException(nameof(url));
        }

        return WithServiceUri(new Uri(url, UriKind.Absolute));
    }

    /// <summary>
    /// Specifies the AWS service URI to use.
    /// </summary>
    /// <param name="uri">The URI to use for AWS services.</param>
    /// <returns>
    /// The current <see cref="AwsClientFactoryBuilder"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="uri"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="uri"/> is not an absolute URI.
    /// </exception>
    public AwsClientFactoryBuilder WithServiceUri(Uri uri)
    {
        if (uri == null)
        {
            throw new ArgumentNullException(nameof(uri));
        }

        if (!uri.IsAbsoluteUri)
        {
            throw new ArgumentException("The AWS service URI must be an absolute URI.", nameof(uri));
        }

        ServiceUri = uri;

        return this;
    }
}
