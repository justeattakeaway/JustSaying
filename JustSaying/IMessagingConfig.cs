using System;
using System.Collections.Generic;

namespace JustSaying
{
    public interface IMessagingConfig 
    {
        IList<string> Regions { get; }
        Func<string> GetActiveRegion { get; set; }

        void Validate();
    }
}