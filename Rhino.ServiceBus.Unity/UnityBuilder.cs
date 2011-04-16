using System;
using Microsoft.Practices.Unity;
using Rhino.ServiceBus.Config;
using Rhino.ServiceBus.Impl;

namespace Rhino.ServiceBus.Unity
{
    public class UnityBuilder : IBusContainerBuilder
    {
        private readonly IUnityContainer container;
        private readonly AbstractRhinoServiceBusConfiguration configuration;

        public UnityBuilder(IUnityContainer container, AbstractRhinoServiceBusConfiguration configuration)
        {
            this.container = container;
            this.configuration = configuration;
            this.configuration.BuildWith(this);
        }

        public void WithInterceptor(IConsumerInterceptor interceptor)
        {
            throw new NotImplementedException();
        }

        public void RegisterDefaultServices()
        {
            throw new NotImplementedException();
        }

        public void RegisterBus()
        {
            throw new NotImplementedException();
        }

        public void RegisterPrimaryLoadBalancer()
        {
            throw new NotImplementedException();
        }

        public void RegisterSecondaryLoadBalancer()
        {
            throw new NotImplementedException();
        }

        public void RegisterReadyForWork()
        {
            throw new NotImplementedException();
        }

        public void RegisterLoadBalancerEndpoint(Uri loadBalancerEndpoint)
        {
            throw new NotImplementedException();
        }

        public void RegisterLoggingEndpoint(Uri logEndpoint)
        {
            throw new NotImplementedException();
        }

        public void RegisterMsmqTransport(Type queueStrategyType)
        {
            throw new NotImplementedException();
        }

        public void RegisterQueueCreation()
        {
            throw new NotImplementedException();
        }

        public void RegisterMsmqOneWay()
        {
            throw new NotImplementedException();
        }

        public void RegisterRhinoQueuesTransport(string path, string name)
        {
            throw new NotImplementedException();
        }

        public void RegisterRhinoQueuesOneWay()
        {
            throw new NotImplementedException();
        }

        public void RegisterSecurity(byte[] key)
        {
            throw new NotImplementedException();
        }

        public void RegisterNoSecurity()
        {
            throw new NotImplementedException();
        }
    }
}