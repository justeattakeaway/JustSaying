using System;
using System.Threading.Tasks;
using JustSaying.Sample.Restaurant.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace JustSaying.Sample.Restaurant.KitchenConsole
{
    internal class Program
    {
        public static async Task Main()
        {
            Console.Title = "KitchenConsole";

            await new HostBuilder()
                .ConfigureAppConfiguration((hostContext, config) =>
                {
                    config.AddJsonFile("appsettings.json", optional: false);
                    config.AddJsonFile($"appsettings.{hostContext.HostingEnvironment.EnvironmentName}.json", optional: true);
                    config.AddEnvironmentVariables();
                })
               .ConfigureLogging(loggingBuilder => loggingBuilder.AddConsole())
               .ConfigureServices((hostContext, services) =>
               {
                   var configuration = hostContext.Configuration;
                   services.AddJustSaying(config =>
                   {
                       config.Client(x =>
                       {
                           if (configuration.HasAWSServiceUrl())
                           {
                               // The AWS client SDK allows specifying a custom HTTP endpoint.
                               // For testing purposes it is useful to specify a value that
                               // points to a docker image such as `p4tin/goaws` or `localstack/localstack`
                               x.WithServiceUri(configuration.GetAWSServiceUri())
                                .WithAnonymousCredentials();
                           }
                           else
                           {
                               // The real AWS environment will require some means of authentication
                               //x.WithBasicCredentials("###", "###");
                               //x.WithSessionCredentials("###", "###", "###");
                           }
                       });

                       config.Messaging(x =>
                       {
                           // Configures which AWS Region to operate in
                           x.WithRegion(configuration.GetAWSRegion());
                       });

                       config.Subscriptions(x =>
                       {
                           // Creates the following if they do not already exist
                           //  - a SQS queue of name `orderplacedevent`
                           //  - a SQS queue of name `orderplacedevent_error`
                           //  - a SNS topic of name `orderplacedevent`
                           //  - a SNS topic subscription on topic 'orderplacedevent' and queue 'orderplacedevent'
                           x.ForTopic<OrderPlacedEvent>();
                       });

                       config.Publications(x =>
                       {
                           // Creates the following if they do not already exist
                           //  - a SNS topic of name `orderreadyevent`
                           x.WithTopic<OrderReadyEvent>();
                       });
                   });

                   // Added a message handler for message type for 'OrderPlacedEvent' on topic 'orderplacedevent' and queue 'orderplacedevent'
                   services.AddJustSayingHandler<OrderPlacedEvent, OrderPlacedEventHandler>();

                   // Add a background service that is listening for messages related to the above subscriptions
                   services.AddHostedService<Subscriber>();
               })
              .UseConsoleLifetime()
              .Build()
              .RunAsync();
        }
    }
}
