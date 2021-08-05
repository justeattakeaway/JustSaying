using System;

namespace JustSaying.AwsTools
{
    public interface IAwsClientFactoryProxy
    {
        IAwsClientFactory GetAwsClientFactory();
    }
}
