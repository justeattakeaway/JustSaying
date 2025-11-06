using System;

namespace JustSaying.AwsTools
{
    public interface IAwsClientFactoryProxy
    {
        IAwsClientFactory GetAwsClientFactory();
        void SetAwsClientFactory(Func<IAwsClientFactory> func);
    }
}