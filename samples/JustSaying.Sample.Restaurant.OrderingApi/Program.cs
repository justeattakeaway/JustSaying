using System;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace JustSaying.Sample.Restaurant.OrderingApi
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            Console.Title = "OrderingApi";

            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args)
        {
            return WebHost.CreateDefaultBuilder(args)
                .ConfigureLogging((loggingBuilder) => loggingBuilder.AddConsole())
                .UseStartup<Startup>();
        }
    }
}
