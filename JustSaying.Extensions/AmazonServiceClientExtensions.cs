using System.ComponentModel;
using System.Net.Http;
using Amazon.Runtime;
using Amazon.Runtime.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace JustSaying.Extensions
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static class AmazonServiceClientExtensions
    {
        internal static void ConfigureHttpHandler<T>(this T client, RuntimePipeline pipeline)
            where T : AmazonServiceClient
        {
            string name = client.GetType().Name;

            var services = new ServiceCollection();

            services
                .AddHttpClient()
                .AddAwsHttpClient(name, client.Config);

            var serviceProvider = services.BuildServiceProvider();
            var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();

            var httpRequestFactory = new CustomHttpRequestFactory(name, client.Config, httpClientFactory);
            var httpHandler = new HttpHandler<HttpContent>(httpRequestFactory, client);

            pipeline.ReplaceHandler<HttpHandler<HttpContent>>(httpHandler);
        }
    }
}
