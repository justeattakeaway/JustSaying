using CommandLine;
using JustSaying.Benchmark;

await Parser.Default.ParseArguments<JustSayingBenchmark>(args)
    .MapResult(async a => await JustSayingBenchmark.RunTest(a),
        errs => Task.CompletedTask);
