using System.Collections.Generic;

namespace JustSaying
{
    public interface IMessagingConfig : IPublishConfiguration //ToDo: This vs publish config. Clean it up. not good.
    {
        IList<string> Regions { get; }

        void Validate();
    }
}