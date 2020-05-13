namespace JustSaying.Messaging.Interrogation
{
    /// <summary>
    /// Provides unstructured interrogation. Implementations of this interface should return an anonymous object that
    /// can be composed together into a root object.
    /// serializable
    /// </summary>
    public interface IInterrogable
    {
        /// <summary>
        /// Interrogates the implementation so that callers can understand the state of the component
        /// </summary>
        /// <returns>An anonymous object with the runtime state of the component</returns>
        object Interrogate();
    }
}
