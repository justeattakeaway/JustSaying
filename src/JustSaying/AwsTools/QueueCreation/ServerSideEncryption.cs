namespace JustSaying.AwsTools.QueueCreation;

public class ServerSideEncryption
{
    public ServerSideEncryption()
    {
        KmsDataKeyReusePeriod = JustSayingConstants.DefaultAttributeEncryptionKeyReusePeriod;
    }

    public string KmsMasterKeyId { get; set; }

    public TimeSpan KmsDataKeyReusePeriod { get; set; }
}
