using Newtonsoft.Json;

namespace JustSaying.AwsTools.QueueCreation
{
    public class ServerSideEncryption
    {
        public ServerSideEncryption()
        {
            KmsMasterKeyId = JustSayingConstants.DefaultAttributeEncryptionKeyId;
            KmsDataKeyReusePeriodSeconds = JustSayingConstants.DefaultAttributeEncryptionKeyReusePeriodSecond;
        }

        [JsonProperty(PropertyName = "kmsMasterKeyId")]
        public string KmsMasterKeyId { get; set; }

        [JsonProperty(PropertyName = "kmsDataKeyReusePeriodSeconds")]
        public string KmsDataKeyReusePeriodSeconds { get; set; }
    }
}
