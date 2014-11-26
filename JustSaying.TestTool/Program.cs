using System;
using Amazon;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Models;

namespace JustSaying.TestTool
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            if (args[0] == "publisher")
            {
                Publisher(args[1]);
            }
            else if (args[0] == "subscriber")
            {
                Subscriber();
            }

            Console.ReadLine();
        }

        private static void Publisher(string region)
        {
            CreateMeABus
                .InRegion(region)
                .WithSqsMessagePublisher<TestMessage>(configuration => { })
                .Publish(new TestMessage {Text = string.Format("Hello from {0}", region)});
        }

        private static void Subscriber()
        {
            var handler = new TestMessageHandler();

            CreateMeABus
                .InRegion(RegionEndpoint.EUWest1.SystemName)
                .WithFailoverRegion("us-east-1")
                .WithSqsPointToPointSubscriber()
                .IntoQueue(string.Empty)
                .ConfigureSubscriptionWith(configuration => { })
                .WithMessageHandler(handler)
                .StartListening();
        }
    }

    public class TestMessageHandler : IHandler<TestMessage>
    {
        public bool Handle(TestMessage message)
        {
            Console.WriteLine(message.Text);
            return true;
        }
    }

    public class TestMessage : Message
    {
        public string Text { get; set; }
    }
}
