using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace JustSaying.Sample.Restaurant.OrderingApi
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            Console.Title = "OrderingApi";

            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureLogging((loggingBuilder) => loggingBuilder.AddConsole())
                .ConfigureWebHostDefaults((builder) => builder.UseStartup<Startup>());
        }
    }
}
