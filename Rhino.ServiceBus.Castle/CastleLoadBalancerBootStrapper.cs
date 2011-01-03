using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.LoadBalancer;

namespace Rhino.ServiceBus.Castle
{
    public sealed class CastleLoadBalancerBootStrapper : LoadBalancerBootStrapper
    {
        private readonly CastleBootStrapper inner;

        public CastleLoadBalancerBootStrapper()
        {
            inner = new CastleBootStrapper();
        }

        public override void CreateContainer()
        {
            inner.CreateContainer();
        }

        protected override AbstractRhinoServiceBusFacility CreateConfiguration()
        {
            return new LoadBalancerFacility();
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
            inner.Dispose();
        }
    }
}