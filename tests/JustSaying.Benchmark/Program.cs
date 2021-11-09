using CommandLine;
using JustSaying.Benchmark;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .MinimumLevel.Warning()
    .WriteTo.Seq("http://localhost:5341")
    .Enrich.WithProperty("AppName", "JustSaying.Benchmark")
    .CreateLogger();

try
{
    await Parser.Default.ParseArguments<JustSayingBenchmark>(args)
        .MapResult(async a => await JustSayingBenchmark.RunTest(a),
            errs => Task.CompletedTask);
}
finally
{
    Log.CloseAndFlush();
}
