using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace JustSaying.AwsTools.QueueCreation
{
    public class ServerSideEncryption
    {
        public ServerSideEncryption()
        {
            KmsMasterKeyId = JustSayingConstants.DefaultSqsAttributeEncryptionKeyId;
            KmsDataKeyReusePeriodSeconds = JustSayingConstants.DefaultAttributeEncryptionKeyReusePeriodSecond;
        }

        [JsonProperty("kmsMasterKeyId")]
        [JsonPropertyName("kmsMasterKeyId")]
        public string KmsMasterKeyId { get; set; }

        [JsonProperty("kmsDataKeyReusePeriodSeconds")]
        [JsonPropertyName("kmsDataKeyReusePeriodSeconds")]
        public string KmsDataKeyReusePeriodSeconds { get; set; }
    }
}
