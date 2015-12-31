using JustSaying.AwsTools;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Messaging.MessageSerialisation;
using JustSaying.Messaging.Monitoring;

namespace JustSaying
{
    public static class CreateMeABus
    {
        public static IMayWantOptionalSettings InRegion(string region)
        {
            return InRegions(region);
        }

        public static IMayWantOptionalSettings InRegions(params string[] regions)
        {
            var config = new MessagingConfig();
            
            if (regions != null)
            foreach (var region in regions)
            {
                config.Regions.Add(region);
            }

            config.Validate();

            var messageSerialisationRegister = new MessageSerialisationRegister();
            var justSayingBus = new JustSayingBus(config, messageSerialisationRegister);

            var awsClientFactory = new DefaultAwsClientFactory();
            var awsClientFactoryProxy = new AwsClientFactoryProxy(() => awsClientFactory);
                
            var amazonQueueCreator = new AmazonQueueCreator(awsClientFactoryProxy);
            var bus = new JustSayingFluently(justSayingBus, amazonQueueCreator, awsClientFactoryProxy);

            bus
                .WithMonitoring(new NullOpMessageMonitor())
                .WithSerialisationFactory(new NewtonsoftSerialisationFactory());

            return bus;
        }
    }
}