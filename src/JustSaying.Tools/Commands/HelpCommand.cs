using System;
using System.Threading.Tasks;

namespace JustSaying.Tools.Commands
{
    public class HelpCommand : ICommand
    {
        public Task<bool> ExecuteAsync()
        {
            Console.WriteLine("Move -from \"sourceUrl\" -to \"destinationUrl\" -in \"region\" -count \"10\"");
            return Task.FromResult(true);
        }
    }
}