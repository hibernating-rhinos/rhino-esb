using Castle.Windsor;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.LoadBalancer;

namespace Rhino.ServiceBus.Castle
{
    public sealed class CastleLoadBalancerBootStrapper : LoadBalancerBootStrapper
    {
        private IWindsorContainer container;
        private CastleBootStrapper inner;

        public override void CreateContainer()
        {
            container = new WindsorContainer();
            inner = new CastleBootStrapper(container);
        }

        protected override AbstractRhinoServiceBusFacility CreateConfiguration()
        {
            return new LoadBalancerFacility();
        }

        protected override void ConfigureBusFacility(AbstractRhinoServiceBusFacility facility)
        {
            facility.UseCastleWindsor(container);
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