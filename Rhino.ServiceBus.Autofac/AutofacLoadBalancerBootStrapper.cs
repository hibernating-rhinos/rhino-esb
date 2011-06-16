using Rhino.ServiceBus.LoadBalancer;

namespace Rhino.ServiceBus.Autofac
{
    public class AutofacLoadBalancerBootStrapper : AutofacBootStrapper
    {
        protected override Impl.AbstractRhinoServiceBusConfiguration CreateConfiguration()
        {
            return new LoadBalancerConfiguration();
        }
    }
}