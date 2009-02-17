using System;
using Castle.Windsor;
using Castle.Windsor.Configuration.Interpreters;
using Rhino.ServiceBus.Impl;

namespace Rhino.ServiceBus.LoadBalancer
{
    public class LoadBalancerHost : MarshalByRefObject, IDisposable
    {
        private MsmqLoadBalancer loadBalancer;

        public override object InitializeLifetimeService()
        {
            return null;
        }

        public void Start()
        {
            var container = new WindsorContainer(new XmlInterpreter());
            container.Kernel.AddFacility("rhino.esb", new RhinoServiceBusFacility());
            container.AddComponent<MsmqLoadBalancer>();

            loadBalancer = container.Resolve<MsmqLoadBalancer>();
        }

        public void Dispose()
        {
            if (loadBalancer != null)
                loadBalancer.Dispose();
        }
    }
}