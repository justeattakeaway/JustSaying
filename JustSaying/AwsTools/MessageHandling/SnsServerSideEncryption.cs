using Newtonsoft.Json;

namespace JustSaying.AwsTools.MessageHandling
{
    public class SnsServerSideEncryption
    {
        public SnsServerSideEncryption()
        {
            KmsMasterKeyId = JustSayingConstants.DefaultSnsAttributeEncryptionKeyId;
        }

        [JsonProperty(PropertyName = "kmsMasterKeyId")]
        public string KmsMasterKeyId { get; set; }
    }
}
