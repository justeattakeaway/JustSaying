using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace JustSaying.Messaging.Interrogation
{
    public interface INotificationSubscriberInterrogation
    {
        ICollection<ISubscriber> Subscribers { get; }
    }
}