using System.Net.Http;
using Amazon.Runtime;
using JustEat.HttpClientInterception;

namespace JustSaying.UnitTests.FakeMessages
{
    public class InterceptingHttpClientFactory : HttpClientFactory
    {
        public InterceptingHttpClientFactory(HttpClientInterceptorOptions options)
        {
            Options = options;
        }

        private HttpClientInterceptorOptions Options { get; }

        public override HttpClient CreateHttpClient(IClientConfig clientConfig)
        {
            return Options.CreateHttpClient();
        }
    }
}
