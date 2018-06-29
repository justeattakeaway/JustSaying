using Newtonsoft.Json;

namespace JustSaying.AwsTools.QueueCreation
{
    public class ServerSideEncryption
    {
        public ServerSideEncryption()
        {
            KmsMasterKeyId = JustSayingConstants.DEFAULT_ATTRIBUTE_ENCRYPTION_KEY_ID;
            KmsDataKeyReusePeriodSeconds = JustSayingConstants.DEFAULT_ATTRIBUTE_ENCRYPTION_KEY_REUSE_PERIOD_SECOND;
        }
            
        [JsonProperty(PropertyName = "kmsMasterKeyId")]
        public string KmsMasterKeyId { get; set; } 

        [JsonProperty(PropertyName = "kmsDataKeyReusePeriodSeconds")]
        public string KmsDataKeyReusePeriodSeconds { get; set; } 
    }
}
