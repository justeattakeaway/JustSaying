namespace JustSaying.Messaging.Interrogation
{
    /// <summary>
    /// Provides an unstructured way of interrogating components. Implementations of this interface return a
    /// serializable
    /// </summary>
    public interface IInterrogable
    {
        object Interrogate();
    }
}
