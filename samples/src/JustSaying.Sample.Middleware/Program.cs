using JustSaying.Messaging;
using JustSaying.Messaging.Middleware;
using JustSaying.Sample.Middleware;
using JustSaying.Sample.Middleware.Extensions;
using JustSaying.Sample.Middleware.Handlers;
using JustSaying.Sample.Middleware.Messages;
using JustSaying.Sample.Middleware.Middlewares;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var host = BuildHost();

await host.StartAsync();

Console.WriteLine("Press enter to publish a message");

var publisher = host.Services.GetService<IMessagePublisher>();

while (true)
{
    var message = new SampleMessage();

    // Uncomment this message to see the Polly middleware in action
    //var message = new UnreliableMessage();

    await publisher.PublishAsync(message);
    Console.ReadLine();
}

static IHost BuildHost()
{
    return new HostBuilder()
        .ConfigureAppConfiguration((_, config) =>
        {
            config.AddJsonFile("appsettings.json", optional: false);
        })
        .ConfigureLogging(logging => logging.AddConsole())
        .ConfigureServices((hostContext, services) =>
        {
            // Register middleware as transient so they can be resolved from DI
            services.AddTransient<InterrogateMiddleware>();
            services.AddTransient<PollyJustSayingMiddleware>();
            
            services.AddJustSaying((config, serviceProvider) =>
            {
                config.Client(x =>
                {
                    if (hostContext.Configuration.HasAWSServiceUrl())
                    {
                        // The AWS client SDK allows specifying a custom HTTP endpoint.
                        // For testing purposes it is useful to specify a value that
                        // points to a docker image such as `localstack/localstack`
                        x.WithServiceUri(hostContext.Configuration.GetAWSServiceUri())
                            .WithAnonymousCredentials();
                    }
                    else
                    {
                        // Explicitly provide AWS credentials
                        //x.WithBasicCredentials("###", "###");
                        //x.WithSessionCredentials("###", "###", "###");
                    }
                });

                config.Messaging(x =>
                {
                    x.WithRegion(hostContext.Configuration.GetAWSRegion());
                });

                config.Subscriptions(x =>
                {
                    x.ForTopic<SampleMessage>((cfg) =>
                    {
                        // Define our middleware pipeline for this subscription
                        // The order middleware are declared is how they will be executed
                        cfg.WithMiddlewareConfiguration(middlewareBuilder =>
                        {
                            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
                            middlewareBuilder.Use(new EchoJustSayingMiddleware(loggerFactory.CreateLogger<EchoJustSayingMiddleware>(), "Outer"));
                            middlewareBuilder.Use<InterrogateMiddleware>();
                            middlewareBuilder.Use(new EchoJustSayingMiddleware(loggerFactory.CreateLogger<EchoJustSayingMiddleware>(), "Inner"));
                            middlewareBuilder.UseDefaults<SampleMessage>(typeof(SampleMessageHandler)); // You should always add UseDefaults to your pipeline as that enforces some default behaviour
                        });
                    });

                    x.ForTopic<UnreliableMessage>((cfg) =>
                    {
                        // The handling of this message may result in some transient errors so we can leverage a Polly middleware here to introduce a backoff strategy
                        cfg.WithMiddlewareConfiguration(middlewareBuilder =>
                        {
                            middlewareBuilder.Use<InterrogateMiddleware>(); // We can share middleware across message types
                            middlewareBuilder.Use<PollyJustSayingMiddleware>();
                            middlewareBuilder.UseDefaults<UnreliableMessage>(typeof(UnreliableMessageHandler));
                        });
                    });
                });

                config.Publications(x =>
                {
                    x.WithTopic<SampleMessage>();
                    x.WithTopic<UnreliableMessage>();
                });
            });

            services.AddJustSayingHandler<SampleMessage, SampleMessageHandler>();
            services.AddJustSayingHandler<UnreliableMessage, UnreliableMessageHandler>();
            services.AddHostedService<JustSayingService>();
        })
        .UseConsoleLifetime()
        .Build();
}
