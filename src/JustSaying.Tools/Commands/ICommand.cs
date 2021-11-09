namespace JustSaying.Tools.Commands;

public interface ICommand
{
    Task<bool> ExecuteAsync();
}