namespace JustSaying
{
    /// <summary>
    /// Indicates that a component is asynchronously startable.
    /// </summary>
    public interface IStartable
    {
        /// <summary>
        /// Starts running the component and returns a task that completes when the component has started.
        /// </summary>
        /// <param name="stoppingToken">A <see cref="CancellationToken"/> that cancels the startup.</param>
        /// <returns>A <see cref="Task"/> that completes when the component has started.</returns>
        Task StartAsync(CancellationToken stoppingToken);
    }
}
