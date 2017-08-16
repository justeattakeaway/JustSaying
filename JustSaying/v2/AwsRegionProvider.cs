using System;
using System.Collections.Generic;

namespace JustSaying.v2
{
    public interface IAwsRegionProvider
    {
        IList<string> AvailableRegions { get; }
        Func<string> GetActiveRegion { get; }
    }

    public class AwsRegionProvider : IAwsRegionProvider
    {
        public IList<string> AvailableRegions { get; }
        public Func<string> GetActiveRegion { get; }

        public AwsRegionProvider(string region, Func<string> activeRegion, params string[] additionalRegions)
        {
            AvailableRegions = new List<string> {region};
            GetActiveRegion = activeRegion;

            foreach (var additionalRegion in additionalRegions)
            {
                AvailableRegions.Add(additionalRegion);
            }
        }

        public AwsRegionProvider(string region, params string[] additionalRegions) : this(region, () => region, additionalRegions)
        {
            
        }
    }
}