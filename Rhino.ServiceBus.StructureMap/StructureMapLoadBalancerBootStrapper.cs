using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.LoadBalancer;
using StructureMap;

namespace Rhino.ServiceBus.StructureMap
{
    public class StructureMapLoadBalancerBootStrapper : LoadBalancerBootStrapper
    {
        private IContainer container;
        private StructureMapBootStrapper inner;

        public override void CreateContainer()
        {
            container = ObjectFactory.Container;
            inner = new StructureMapBootStrapper(container);
        }

        protected override AbstractRhinoServiceBusConfiguration CreateConfiguration()
        {
            return new LoadBalancerConfiguration();
        }

        public override void ExecuteDeploymentActions(string user)
        {
            inner.ExecuteDeploymentActions(user);
        }

        public override void ExecuteEnvironmentValidationActions()
        {
            inner.ExecuteEnvironmentValidationActions();
        }

        public override T GetInstance<T>()
        {
            return inner.GetInstance<T>();
        }

        public override void Dispose()
        {
            container.Dispose();
        }
    }
}