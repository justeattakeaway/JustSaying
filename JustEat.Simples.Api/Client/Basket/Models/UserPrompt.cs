using System.Collections.Generic;

namespace JustEat.Simples.Api.Client.Basket.Models
{
    public class UserPrompt
    {
        public UserPrompt(string defaultMessage, string statusCode, List<MessageKeyValue> keyValues)
        {
            DefaultMessage = defaultMessage;
            StatusCode = statusCode;
            MessageValues = keyValues;
        }

        public string DefaultMessage { get; private set; }
        public string StatusCode { get; private set; }
        public IList<MessageKeyValue> MessageValues { get; private set; }
    }
}
