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
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .MinimumLevel.Information()
    .CreateLogger();

try
{
    var host = BuildHost();

    await host.StartAsync();

    Log.Information("Press enter to publish a message");

    var publisher = host.Services.GetService<IMessagePublisher>();

    while (true)
    {
        var message = new SampleMessage();

        // Uncomment this message to see the Polly middleware in action
        //var message = new UnreliableMessage();

        await publisher.PublishAsync(message);
        Console.ReadLine();
    }
}
catch (Exception e)
{
    Log.Fatal(e, "Error occurred during startup: {Message}", e.Message);
}
finally
{
    Log.CloseAndFlush();

    Console.Title = string.Empty;
}

static IHost BuildHost()
{
    return new HostBuilder()
        .ConfigureAppConfiguration((_, config) =>
        {
            config.AddJsonFile("appsettings.json", optional: false);
        })
        .UseSerilog()
        .ConfigureServices((hostContext, services) =>
        {
            services.AddJustSaying(config =>
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
                            middlewareBuilder.Use(new EchoJustSayingMiddleware("Outer"));
                            middlewareBuilder.Use(new InterrogateMiddleware());
                            middlewareBuilder.Use(new EchoJustSayingMiddleware("Inner"));
                            middlewareBuilder.UseDefaults<SampleMessage>(typeof(SampleMessageHandler)); // You should always add UseDefaults to your pipeline as that enforces some default behaviour
                        });
                    });

                    x.ForTopic<UnreliableMessage>((cfg) =>
                    {
                        // The handling of this message may result in some transient errors so we can leverage a Polly middleware here to introduce a backoff strategy
                        cfg.WithMiddlewareConfiguration((middlewareBuilder) =>
                        {
                            middlewareBuilder.Use(new InterrogateMiddleware()); // We can share middleware across message types
                            middlewareBuilder.Use(new PollyJustSayingMiddleware());
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
