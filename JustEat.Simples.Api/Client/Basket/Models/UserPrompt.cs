using System.Collections.Generic;

namespace JustEat.Simples.Api.Client.Basket.Models
{
    public class UserPrompt
    {
        public string DefaultMessage { get; private set; }
        public string StatusCode { get; private set; }
        public IList<MessageKeyValue> MessageValues { get; private set; }
    }
}
