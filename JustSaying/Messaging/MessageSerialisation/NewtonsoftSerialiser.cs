using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JustSaying.Messaging.Compression;
using JustSaying.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace JustSaying.Messaging.MessageSerialisation
{
    public class NewtonsoftSerialiser : IMessageSerialiser
    {
        private readonly JsonSerializerSettings _settings;
        private Dictionary<string, IMessageBodyCompression> _compression;

        private static readonly JsonSerializerSettings DefaultSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            Converters = new JsonConverter[] { new Newtonsoft.Json.Converters.StringEnumConverter() }
        };

        public NewtonsoftSerialiser() : this(null)
        {
        }

        public NewtonsoftSerialiser(JsonSerializerSettings settings)
        {
            _settings = settings ?? DefaultSettings;
            _compression = new Dictionary<string, IMessageBodyCompression>();
        }

        public NewtonsoftSerialiser AddCompression(string headerIdentifier, IMessageBodyCompression compression)
        {
            _compression.Add(headerIdentifier, compression);
            return this;
        }

        public NewtonsoftSerialiser AddCompressions(Dictionary<string, IMessageBodyCompression> compressions)
        {
            _compression = compressions;
            return this;
        }

        public Message Deserialise(string message, Type type)
        {
            var messageBody = ExtractTokenValue(message, "Message");

            if (string.IsNullOrEmpty(messageBody))
            {
                throw new JsonException("The 'Message' property was not found or is empty in the JSON payload.");
            }

            if (_compression.Count <= 0)
            {
                return (Message)JsonConvert.DeserializeObject(messageBody, type, _settings);
            }

            foreach (var compressor in _compression)
            {
                if (!messageBody.StartsWith(compressor.Key, StringComparison.Ordinal))
                {
                    continue;
                }

                messageBody = compressor.Value.Decompress(messageBody);
                break;
            }

            return (Message)JsonConvert.DeserializeObject(messageBody, type, _settings);
        }

        public string Serialise(Message message, bool serializeForSnsPublishing, string subject)
        {
            return Serialise(message, serializeForSnsPublishing, false, subject);
        }

        public string Serialise(object message, bool serializeForSnsPublishing, bool withCompression, string subject)
        {
            var msg = JsonConvert.SerializeObject(message, _settings);

            if (withCompression)
            {
                _compression.TryGetValue(CompressedHeaders.GzipBase64Header, out var compression);
                msg = compression?.Compress(msg);
            }

            // AWS SNS service will add Subject and Message properties automatically,
            // so just return plain message
            if (serializeForSnsPublishing)
            {
                return msg;
            }

            // for direct publishing to SQS, add Subject and Message properties manually
            var context = new { Subject = subject, Message = msg };
            return JsonConvert.SerializeObject(context);
        }

        public string GetMessageSubject(string sqsMessage)
        {
            return ExtractTokenValue(sqsMessage, "Subject") ?? string.Empty;
        }

        private static string ExtractTokenValue(string json, string tokenName)
        {
            using (var stringReader = new StringReader(json))
            {
                using (var jsonReader = new JsonTextReader(stringReader))
                {
                    while (jsonReader.Read())
                    {
                        if (jsonReader.TokenType != JsonToken.PropertyName ||
                            (string)jsonReader.Value != tokenName)
                        {
                            continue;
                        }

                        if (jsonReader.Read())
                        {
                            return jsonReader.Value?.ToString();
                        }
                    }
                }
            }
            return null;
        }
    }


}
