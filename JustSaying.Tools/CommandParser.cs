using System.Linq;
using JustSaying.Tools.Commands;
using Magnum.CommandLineParser;
using Magnum.Monads.Parser;

namespace JustSaying.Tools
{
    public class CommandParser
    {
        public bool Parse(string commandText)
        {
            return CommandLine
                .Parse<ICommand>(commandText, InitializeCommandLineParser)
                .All(option => option.Execute());
        }

        private static void InitializeCommandLineParser(ICommandLineElementParser<ICommand> x)
        {
            x.Add((from arg in x.Argument("exit")
                   select (ICommand) new ExitCommand())
                .Or(from arg in x.Argument("quit")
                    select (ICommand) new ExitCommand())
                .Or(from arg in x.Argument("help")
                    select (ICommand) new HelpCommand())

               .Or(from arg in x.Argument("move")
                   from sourceQueueName in x.Definition("from")
                   from destinationQueueName in x.Definition("to")
                   from region in x.Definition("in")
                   from count in (from d in x.Definition("count") select d).Optional("count", "1")
                   select (ICommand)new MoveCommand(sourceQueueName.Value, destinationQueueName.Value, region.Value, int.Parse(count.Value)))
                );
        }
    }
}
