using System.Threading.Tasks;
using JustSaying.Tools;
using Magnum.CommandLineParser;
using Magnum.Extensions;

var line = CommandLine.GetUnparsedCommandLine().Trim();
if (line.IsNotEmpty())
{
    await ProcessLine(line).ConfigureAwait(false);
}

static async Task<bool> ProcessLine(string line)
{
    return await CommandParser.ParseAndExecuteAsync(line).ConfigureAwait(false);
}
