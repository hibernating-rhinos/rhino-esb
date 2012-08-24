using System;

namespace Rhino.ServiceBus.Config
{
    public interface IRhinoQueuesBusContainerBuilder
    {
        void RegisterRhinoQueuesTransport();
        void RegisterRhinoQueuesOneWay();
    }
}