using System;

namespace JustSaying.AwsTools
{
    public class AwsClientFactoryProxy : IAwsClientFactoryProxy
    {
        private Func<IAwsClientFactory> awsClientFactoryFunc;

        public AwsClientFactoryProxy()
        {
            awsClientFactoryFunc = () => new DefaultAwsClientFactory();
        }

        public AwsClientFactoryProxy(Func<IAwsClientFactory> awsClientFactoryFunc)
        {
            this.awsClientFactoryFunc = awsClientFactoryFunc;
        }

        public IAwsClientFactory GetAwsClientFactory()
        {
            return awsClientFactoryFunc();
        }

        public void SetAwsClientFactory(Func<IAwsClientFactory> func)
        {
            awsClientFactoryFunc = func;
        }
    }
}