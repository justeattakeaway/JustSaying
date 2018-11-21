using System.Threading.Tasks;
using Magnum.CommandLineParser;
using Magnum.Extensions;

namespace JustSaying.Tools
{
    public static class Program
    {
        public static async Task Main()
        {
            var line = CommandLine.GetUnparsedCommandLine().Trim();
            if (line.IsNotEmpty())
            {
                await ProcessLine(line).ConfigureAwait(false);
            }
        }

        private static async Task<bool> ProcessLine(string line)
        {
            var commandParser = new CommandParser();
            return await commandParser.ParseAndExecuteAsync(line).ConfigureAwait(false);
        }
    }
}
