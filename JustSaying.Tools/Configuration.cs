using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;

namespace JustSaying.Tools
{
    public class Configuration
    {
        public Configuration()
        {
            var awsConfig = string.Format("{0}/.aws/config", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
            var confinternal = new ExpandoObject() as IDictionary<string, object>;
            if (File.Exists(awsConfig))
            {
                using (var str = new StreamReader(awsConfig))
                {
                    string line = null;
                    do
                    {
                        line = str.ReadLine();
                        if (line == null)
                            continue;
                        var parts = line.Split(new[] { '=' });
                        if (parts.Count() == 2)
                        {
                            confinternal.Add(parts[0].Trim(), parts[1].Trim());
                        }

                    } while (line != null);
                }
            }
            dynamic conf = confinternal;
            Region = conf.region;
            AWSAccessKey = conf.aws_access_key_id;
            AWSSecretKey = conf.aws_secret_access_key;
        }
        public string AWSAccessKey { get; private set; }
        public string AWSSecretKey { get; private set; }
        public string Region { get; private set; }
    }
}
