using System;
using System.Collections.Generic;
using System.Reflection;

namespace Rhino.ServiceBus.Config
{
    public interface IBusContainerBuilder
    {
        void WithInterceptor(IConsumerInterceptor interceptor);
        void RegisterDefaultServices(IEnumerable<Assembly> assemblies);
        void RegisterBus();
        void RegisterPrimaryLoadBalancer();
        void RegisterSecondaryLoadBalancer();
        void RegisterReadyForWork();
        void RegisterLoadBalancerEndpoint(Uri loadBalancerEndpoint);
        void RegisterLoggingEndpoint(Uri logEndpoint);
        void RegisterMsmqTransport(Type queueStrategyType);
        void RegisterQueueCreation();
        void RegisterMsmqOneWay();
        void RegisterSecurity(byte[] key);
        void RegisterNoSecurity();
    }
}