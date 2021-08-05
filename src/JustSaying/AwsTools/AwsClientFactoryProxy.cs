using System;

namespace JustSaying.AwsTools
{
    public class AwsClientFactoryProxy : IAwsClientFactoryProxy
    {
        private readonly Func<IAwsClientFactory> _awsClientFactoryFunc;

        public AwsClientFactoryProxy()
        {
            _awsClientFactoryFunc = () => new DefaultAwsClientFactory();
        }

        public AwsClientFactoryProxy(Func<IAwsClientFactory> awsClientFactoryFunc)
        {
            _awsClientFactoryFunc = awsClientFactoryFunc;
        }

        public AwsClientFactoryProxy(Lazy<IAwsClientFactory> factory)
        {
            _awsClientFactoryFunc = () => factory.Value;
        }

        public IAwsClientFactory GetAwsClientFactory() => _awsClientFactoryFunc();
    }
}
