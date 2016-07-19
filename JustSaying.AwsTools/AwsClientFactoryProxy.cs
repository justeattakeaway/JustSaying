using System;

namespace JustSaying.AwsTools
{
    public class AwsClientFactoryProxy : IAwsClientFactoryProxy
    {
        private Func<IAwsClientFactory> _awsClientFactoryFunc;

        public AwsClientFactoryProxy()
        {
            _awsClientFactoryFunc = () => new DefaultAwsClientFactory();
        }

        public AwsClientFactoryProxy(Func<IAwsClientFactory> awsClientFactoryFunc)
        {
            _awsClientFactoryFunc = awsClientFactoryFunc;
        }

        public IAwsClientFactory GetAwsClientFactory() => _awsClientFactoryFunc();

        public void SetAwsClientFactory(Func<IAwsClientFactory> func) => _awsClientFactoryFunc = func;
    }
}