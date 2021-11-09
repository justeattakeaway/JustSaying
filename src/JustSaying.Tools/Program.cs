using JustSaying.Tools;
using Magnum.CommandLineParser;
using Magnum.Extensions;

var line = CommandLine.GetUnparsedCommandLine().Trim();
if (line.IsNotEmpty())
{
    await ProcessLine(line).ConfigureAwait(false);
}

static async Task ProcessLine(string line)
{
    await CommandParser.ParseAndExecuteAsync(line).ConfigureAwait(false);
}
