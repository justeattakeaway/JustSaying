using Newtonsoft.Json;

namespace JustSaying.Messaging.Interrogation
{
    /// <summary>
    /// This type represents the result of interrogating a bus component.
    /// </summary>
    [JsonConverter(typeof(InterrogationResultJsonConverter))]
    public class InterrogationResult
    {
        public InterrogationResult(object data)
        {
            Data = data;
        }

        /// <summary>
        /// Serialize this to JSON and log it on startup to gain some valuable insights into what's
        /// going on inside JustSaying.
        /// Note: This property is intentionally untyped, because it is unstructured and subject to change.
        /// It should only be used for diagnostic purposes.
        /// </summary>
        public object Data { get; }
    }
}
