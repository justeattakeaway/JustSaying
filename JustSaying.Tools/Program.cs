using Magnum.CommandLineParser;
using Magnum.Extensions;

namespace JustSaying.Tools
{
    class Program
    {
        static void Main()
        {
            string line = CommandLine.GetUnparsedCommandLine().Trim();
            if (line.IsNotEmpty())
            {
                ProcessLine(line);
            }
        }

        static bool ProcessLine(string line)
        {
            var commandParser = new CommandParser(new Configuration());
            return commandParser.Parse(line);
        }

        public static string CurrentUri { get; set; }
    }
}
