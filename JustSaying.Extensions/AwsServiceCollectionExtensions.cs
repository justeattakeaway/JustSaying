using System.ComponentModel;
using System.Net;
using System.Net.Http;
using Amazon.Runtime;
using Microsoft.Extensions.DependencyInjection;

namespace JustSaying.Extensions
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static class AwsServiceCollectionExtensions
    {
        internal static IHttpClientBuilder AddAwsHttpClient(this IServiceCollection services, string name, IClientConfig clientConfig)
        {
            // Based on https://github.com/aws/aws-sdk-net/blob/b691e46e57a3e24477e6a5fa2e849da44db7002f/sdk/src/Core/Amazon.Runtime/Pipeline/HttpHandler/_mobile/HttpRequestMessageFactory.cs#L164-L202
            return services
                .AddHttpClient(name)
                .ConfigurePrimaryHttpMessageHandler(() =>
                {
                    var handler = new HttpClientHandler()
                    {
                        AllowAutoRedirect = clientConfig.AllowAutoRedirect,
                        AutomaticDecompression = DecompressionMethods.None,
                    };

                    if (clientConfig.MaxConnectionsPerServer.HasValue)
                    {
                        handler.MaxConnectionsPerServer = clientConfig.MaxConnectionsPerServer.Value;
                    }

                    IWebProxy proxy = clientConfig.GetWebProxy();

                    if (proxy != null)
                    {
                        handler.Proxy = proxy;
                    }

                    if (handler.Proxy != null && clientConfig.ProxyCredentials != null)
                    {
                        handler.Proxy.Credentials = clientConfig.ProxyCredentials;
                    }

                    return handler;
                })
                .ConfigureHttpClient((httpClient) =>
                {
                    if (clientConfig.Timeout.HasValue)
                    {
                        httpClient.Timeout = clientConfig.Timeout.Value;
                    }
                });
        }
    }
}
