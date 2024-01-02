using Amazon.SimpleNotificationService;
using Amazon.SQS;
using JustSaying;
using JustSaying.AwsTools;
using JustSaying.Extensions.DependencyInjection.AwsCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.ComponentModel;

namespace Microsoft.Extensions.DependencyInjection;
/// <summary>
/// A class containing extension methods for the <see cref="IServiceCollection"/> interface. This class cannot be inherited.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class IServiceCollectionExtensions
{
    /// <summary>
    /// Adds JustSaying services to the service collection.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add JustSaying services to.</param>
    /// <param name="configuration">The <see cref="IConfiguration"/> used to setup AWS configuration.</param>
    /// <param name="configure">A delegate to a method to use to configure JustSaying.</param>
    /// <returns>
    /// The <see cref="IServiceCollection"/> specified by <paramref name="services"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="services"/> is <see langword="null"/>.
    /// </exception>
    public static void AddJustSyaingWithAwsConfig(this IServiceCollection services, IConfiguration configuration, Action<MessagingBusBuilder> builderConfig)
    {
        AddJustSayingWithAwsConfig(services, configuration,(builder,_) => builderConfig.Invoke(builder));
    }

    /// <summary>
    /// Adds JustSaying services to the service collection.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add JustSaying services to.</param>
    /// <param name="configuration">The <see cref="IConfiguration"/> used to setup AWS configuration.</param>
    /// <param name="configure">A delegate to a method to use to configure JustSaying.</param>
    /// <returns>
    /// The <see cref="IServiceCollection"/> specified by <paramref name="services"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="services"/> is <see langword="null"/>.
    /// </exception>
    public static void AddJustSayingWithAwsConfig(this IServiceCollection serviceCollection, IConfiguration configuration, Action<MessagingBusBuilder, IServiceProvider> builderConfig)
    {
        serviceCollection.AddDefaultAWSOptions(configuration.GetAWSOptions());
        serviceCollection.TryAddSingleton<IAwsClientFactory, AwsConfigResolvingClientFactory>();
        serviceCollection.AddAWSService<IAmazonSimpleNotificationService>(ServiceLifetime.Transient);
        serviceCollection.AddAWSService<IAmazonSQS>(ServiceLifetime.Transient);
        serviceCollection.AddJustSaying(builderConfig);
    }
}
