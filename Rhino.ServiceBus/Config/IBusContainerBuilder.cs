using System;

namespace Rhino.ServiceBus.Config
{
    public interface IBusContainerBuilder
    {
        void RegisterDefaultServices();
        void RegisterBus();
        void RegisterPrimaryLoadBalancer();
        void RegisterSecondaryLoadBalancer();
        void RegisterReadyForWork();
        void RegisterLoadBalancerEndpoint(Uri loadBalancerEndpoint);
        void RegisterLoggingEndpoint(Uri logEndpoint);
        void RegisterMsmqTransport(Type queueStrategyType);
        void RegisterQueueCreation();
        void RegisterMsmqOneWay();
        void RegisterRhinoQueuesTransport(string path, string name);
        void RegisterRhinoQueuesOneWay();
        void RegisterSecurity(byte[] key);
        void RegisterNoSecurity();
    }
}