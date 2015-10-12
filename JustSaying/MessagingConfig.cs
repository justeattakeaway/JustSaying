using System;
using System.Collections.Generic;
using System.Linq;

namespace JustSaying
{
    public class MessagingConfig : IMessagingConfig
    {
        public MessagingConfig()
        {
            Regions = new List<string>();
        }

        public IList<string> Regions { get; private set; }
        public Func<string> GetActiveRegion { get; set; }

        public virtual void Validate()
        {
            if (!Regions.Any() || string.IsNullOrWhiteSpace(Regions.First()))
            {
                throw new ArgumentNullException("config.Regions", "Cannot have a blank entry for config.Regions");
            }
            var duplicateRegion = Regions.GroupBy(x => x).FirstOrDefault(y => y.Count() > 1);
            if (duplicateRegion != null)
                throw new ArgumentException(string.Format("Region {0} was added multiple times", duplicateRegion.Key));
        
        }
    }
}