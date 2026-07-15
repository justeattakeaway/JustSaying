using JustSaying.Extensions.HeaderPropagation;
using JustSaying.Messaging;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for registering JustSaying header propagation.
/// </summary>
public static class JustSayingHeaderPropagationServiceCollectionExtensions
{
    /// <summary>
    /// Registers a <see cref="HeaderPropagationPublisher"/> decorator that propagates the specified HTTP
    /// request headers as SNS/SQS message attributes on every publish call.
    /// </summary>
    /// <remarks>
    /// Must be called after <c>AddJustSaying()</c> so that the <see cref="IMessagePublisher"/> descriptor
    /// already exists. <see cref="Microsoft.AspNetCore.Http.IHttpContextAccessor"/> must also be registered
    /// (e.g. via <c>services.AddHttpContextAccessor()</c>).
    /// </remarks>
    /// <param name="services">The <see cref="IServiceCollection"/> to configure.</param>
    /// <param name="headers">The HTTP header names to propagate as message attributes.</param>
    /// <returns>The <see cref="IServiceCollection"/> for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/>.</exception>
    public static IServiceCollection AddJustSayingHeaderPropagation(
        this IServiceCollection services,
        params string[] headers)
    {
        if (services is null) throw new ArgumentNullException(nameof(services));

        services.AddSingleton(new HeaderPropagationOptions(headers));

        var descriptor = services.LastOrDefault(d => d.ServiceType == typeof(IMessagePublisher));
        if (descriptor?.ImplementationFactory is { } originalFactory)
        {
            services.Remove(descriptor);
            services.AddSingleton<IMessagePublisher>(sp =>
                new HeaderPropagationPublisher(
                    (IMessagePublisher)originalFactory(sp),
                    sp.GetRequiredService<Microsoft.AspNetCore.Http.IHttpContextAccessor>(),
                    sp.GetRequiredService<HeaderPropagationOptions>()));
        }

        return services;
    }
}
