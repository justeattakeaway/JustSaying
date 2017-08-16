using System;
using JustSaying.AwsTools;

namespace JustSaying.v2.FluentApi
{
    public interface IAwsMessageBusConfiguration
    {
        void UseRegions(string region, Func<string> activeRegion, params string[] additionalRegions);
        void UseRegions(string region, params string[] additionalRegions);
        void UseRegions(IAwsRegionProvider regionProvider);
        void ConfigureDependencies(Action<IAwsDependencies> dependenciesCfg);
    }

    public interface IAwsMessageBusBuilder
    {
        IAwsMessagePublisher CreatePublishers(int publishFailureReAttempts, int publishFailureBackoffMilliseconds);
        IAwsMessageSubscriber CreateSubscribers();
    }

    public class AwsMessageBusBuilder : IAwsMessageBusConfiguration, IAwsMessageBusBuilder
    {
        protected IAwsDependencies Dependencies { get; private set; }
        protected IAwsRegionProvider RegionProvider { get; private set; }

        public void UseRegions(string region, Func<string> activeRegion, params string[] additionalRegions) => RegionProvider = new AwsRegionProvider(region, activeRegion, additionalRegions);

        public void UseRegions(string region, params string[] additionalRegions) => RegionProvider = new AwsRegionProvider(region, additionalRegions);

        public void UseRegions(IAwsRegionProvider regionProvider) => RegionProvider = regionProvider;

        public void ConfigureDependencies(Action<IAwsDependencies> dependenciesCfg)
        {
            Dependencies = new AwsDependencies();
            dependenciesCfg.Invoke(Dependencies);
        }

        public IAwsMessagePublisher CreatePublishers(int publishFailureReAttempts, int publishFailureBackoffMilliseconds)
        {
            return new AwsMessagePublisher(publishFailureReAttempts, publishFailureBackoffMilliseconds, RegionProvider, new AwsClientFactoryProxy(() => Dependencies.AwsClientFactory), Dependencies.NamingStrategy, Dependencies.SerialisationRegister, Dependencies.SerialisationFactory, Dependencies.MessageMonitor, Dependencies.LoggerFactory);
        }

        public IAwsMessageSubscriber CreateSubscribers()
        {
            return new AwsMessageSubscriber();
        }
    }
}