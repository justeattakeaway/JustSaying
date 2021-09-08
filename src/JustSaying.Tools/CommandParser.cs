using System.Globalization;
using System.Threading.Tasks;
using JustSaying.Tools.Commands;
using Magnum.CommandLineParser;
using Magnum.Monads.Parser;

namespace JustSaying.Tools
{
    public static class CommandParser
    {
        public static async Task<bool> ParseAndExecuteAsync(string commandText)
        {
            var anyCommandFailure = false;

            await CommandLine
                .Parse<ICommand>(commandText, InitializeCommandLineParser)
                .ForEachAsync(async option =>
                {
                    var optionSuccess = await option.ExecuteAsync().ConfigureAwait(false);
                    anyCommandFailure |= !optionSuccess;
                }).ConfigureAwait(false);

            return anyCommandFailure;
        }

        private static void InitializeCommandLineParser(ICommandLineElementParser<ICommand> x)
        {
            x.Add((from arg in x.Argument("exit")
                   select (ICommand)new ExitCommand())
                .Or(from arg in x.Argument("quit")
                    select (ICommand)new ExitCommand())
                .Or(from arg in x.Argument("help")
                    select (ICommand)new HelpCommand())
            );
        }
    }
}
