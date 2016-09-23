using System;

namespace JustSaying.Tools.Commands
{
    public class HelpCommand : ICommand
    {
        public bool Execute()
        {
            Console.WriteLine("Move -from \"sourceUrl\" -to \"destinationUrl\" -in \"region\" -count \"10\"");
            return true;
        }
    }
}