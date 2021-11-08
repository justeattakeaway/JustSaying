namespace JustSaying.Tools.Commands;

public class QuitCommand : ICommand
{
    public Task<bool> ExecuteAsync() => Task.FromResult(true);
}