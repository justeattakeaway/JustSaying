using JustSaying.Sample.Restaurant.OrderingApi;
using Serilog;
using Serilog.Events;

const string appName = "OrderingApi";
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .MinimumLevel.Debug()
    .Enrich.WithProperty("AppName", appName)
    .CreateLogger();

Console.Title = "OrderingApi";

try
{
    CreateHostBuilder(args).Build().Run();
}
catch (Exception e)
{
    Log.Fatal(e, "Error occurred during startup: {Message}", e.Message);
}
finally
{
    Log.CloseAndFlush();
}

static IHostBuilder CreateHostBuilder(string[] args)
{
    return Host.CreateDefaultBuilder(args)
        .UseSerilog()
        .ConfigureWebHostDefaults((builder) => builder.UseStartup<Startup>());
}
