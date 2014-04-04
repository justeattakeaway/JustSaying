using Amazon.EC2.Util;

namespace JustSaying.AwsTools
{
    public class InstanceMetaData
    {
        public string PrivateIpAddress()
        {
            return EC2Metadata.PrivateIpAddress;
        }

        public string LocalHostname()
        {
            return EC2Metadata.LocalHostname;
        }
    }
}