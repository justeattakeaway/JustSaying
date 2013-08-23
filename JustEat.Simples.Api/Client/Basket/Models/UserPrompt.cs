using System.Collections.Generic;

namespace JustEat.Simples.Api.Client.Basket.Models
{
    public class UserPrompt
    {
        public string DefaultMessage { get; set; }
        public string StatusCode { get; set; }
        public IList<MessageKeyValue> MessageValues { get; set; }
    }
}
