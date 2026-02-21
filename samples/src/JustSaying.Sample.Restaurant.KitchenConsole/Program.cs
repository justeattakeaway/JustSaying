using JustSaying.Messaging.Middleware;
using JustSaying.Sample.Restaurant.KitchenConsole;
using JustSaying.Sample.Restaurant.KitchenConsole.Handlers;
using JustSaying.Sample.Restaurant.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

const string appName = "KitchenConsole";

Console.Title = appName;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();

var configuration = builder.Configuration;
builder.Services.AddJustSaying(config =>
{
    config.Client(x =>
    {
        if (configuration.HasAWSServiceUrl())
        {
            // The AWS client SDK allows specifying a custom HTTP endpoint.
            // For testing purposes it is useful to specify a value that
            // points to a docker image such as `localstack/localstack`
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
        x.WithSubscriptionGroup("GroupA",
            c => c.WithPrefetch(10)
                .WithMultiplexerCapacity(10)
                .WithConcurrencyLimit(5));

        // Creates the following if they do not already exist
        //  - a SQS queue of name `orderplacedevent`
        //  - a SQS queue of name `orderplacedevent_error`
        //  - a SNS topic of name `orderplacedevent`
        //  - a SNS topic subscription on topic 'orderplacedevent' and queue 'orderplacedevent' with two tags
        //      - "IsOrderEvent" with no value
        //      - "Subscriber" with the value "KitchenConsole"
        //  - a SNS topic subscription on topic 'orderonitswayevent' and queue 'orderonitswayevent'
        x.ForTopic<OrderPlacedEvent>(cfg =>
            cfg.WithTag("IsOrderEvent")
                .WithTag("Subscriber", appName)
                .WithReadConfiguration(rc =>
                    rc.WithSubscriptionGroup("GroupA"))
                .WithMiddlewareConfiguration(pipe =>
                {
                    pipe.UseDefaults<OrderPlacedEvent>(typeof(OrderPlacedEventHandler));
                }));

        x.ForTopic<OrderOnItsWayEvent>(cfg =>
            cfg.WithReadConfiguration(rc =>
                rc.WithSubscriptionGroup("GroupB"))
                .WithMiddlewareConfiguration(pipe =>
                {
                    pipe.UseDefaults<OrderOnItsWayEvent>(typeof(OrderOnItsWayEventHandler));
                }));
    });

    config.Publications(x =>
    {
        // Creates the following if they do not already exist
        //  - an SNS topic of name `orderreadyevent` with two tags:
        //      - "IsOrderEvent" with no value
        //      - "Publisher" with the value "KitchenConsole"
        x.WithTopic<OrderReadyEvent>(cfg =>
        {
            cfg.WithTag("IsOrderEvent")
                .WithTag("Publisher", appName);
        });
        x.WithTopic<OrderDeliveredEvent>();
    });
});

// Added a message handler for message type for 'OrderPlacedEvent' on topic 'orderplacedevent' and queue 'orderplacedevent'
builder.Services.AddJustSayingHandler<OrderPlacedEvent, OrderPlacedEventHandler>();
builder.Services.AddJustSayingHandler<OrderOnItsWayEvent, OrderOnItsWayEventHandler>();

// Add a background service that is listening for messages related to the above subscriptions
builder.Services.AddHostedService<Subscriber>();

var host = builder.Build();
await host.RunAsync();
