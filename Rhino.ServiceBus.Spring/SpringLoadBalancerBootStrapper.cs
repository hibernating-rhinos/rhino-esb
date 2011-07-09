using Rhino.ServiceBus.LoadBalancer;

namespace Rhino.ServiceBus.Spring
{
    public sealed class SpringLoadBalancerBootStrapper : SpringBootStrapper
    {
        protected override Impl.AbstractRhinoServiceBusConfiguration CreateConfiguration()
        {
            return new LoadBalancerConfiguration();
        }
    }
}