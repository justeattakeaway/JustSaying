using System.Linq;
using JustSaying.Tools.Commands;
using Magnum.CommandLineParser;
using Magnum.Monads.Parser;

namespace JustSaying.Tools
{
    public class CommandParser
	{
        private readonly Configuration _configuration;

        public CommandParser(Configuration configuration)
        {
            _configuration = configuration;
        }

        public  bool Parse(string commandText)
		{
			return CommandLine.Parse<ICommand>(commandText, InitializeCommandLineParser)
				.All(option =>
				{
				    return option.Execute();
				});
		}

		 void InitializeCommandLineParser(ICommandLineElementParser<ICommand> x)
		{
			x.Add((from arg in x.Argument("exit")
			       select (ICommand) new ExitCommand())
				.Or(from arg in x.Argument("quit")
				    select (ICommand) new ExitCommand())
				.Or(from arg in x.Argument("help")
				    select (ICommand) new HelpCommand())
				
				.Or(from arg in x.Argument("move")
				    from fromUri in x.Definition("from")
				    from toUri in x.Definition("to")
				    from count in
				    	(from d in x.Definition("count") select d).Optional("count", "1")
				    select (ICommand) new MoveCommand(fromUri.Value, toUri.Value, int.Parse(count.Value), _configuration))
				);
		}
	}
}
