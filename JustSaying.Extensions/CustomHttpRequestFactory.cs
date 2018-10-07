using System;
using System.Net.Http;
using Amazon.Runtime;

namespace JustSaying.Extensions
{
    internal sealed class CustomHttpRequestFactory : IHttpRequestFactory<HttpContent>
    {
        public CustomHttpRequestFactory(string name, IClientConfig config, IHttpClientFactory factory)
        {
            Name = name;
            Config = config;
            Factory = factory;
        }

        private IClientConfig Config { get; }

        private IHttpClientFactory Factory { get; }

        private string Name { get; }

        public IHttpRequest<HttpContent> CreateHttpRequest(Uri requestUri)
        {
            var httpClient = Factory.CreateClient(Name);
            return new HttpWebRequestMessage(httpClient, requestUri, Config);
        }

        public void Dispose()
        {
            // Nothing to do
        }
    }
}
