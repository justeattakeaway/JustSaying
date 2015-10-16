using Magnum.CommandLineParser;
using Magnum.Extensions;

namespace JustSaying.Tools
{
    public static class Program
    {
        public static void Main()
        {
            var line = CommandLine.GetUnparsedCommandLine().Trim();
            if (line.IsNotEmpty())
            {
                ProcessLine(line);
            }
        }

        private static bool ProcessLine(string line)
        {
            var commandParser = new CommandParser();
            return commandParser.Parse(line);
        }
    }
}
