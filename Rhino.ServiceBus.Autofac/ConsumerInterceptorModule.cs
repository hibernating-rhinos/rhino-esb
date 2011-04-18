using Autofac;
using Autofac.Core;
using Autofac.Core.Lifetime;
using Rhino.ServiceBus.Config;
using Rhino.ServiceBus.Internal;

namespace Rhino.ServiceBus
{
    public class ConsumerInterceptorModule : Module
    {
        private readonly IConsumerInterceptor consumerInterceptor;

        public ConsumerInterceptorModule(IConsumerInterceptor consumerInterceptor)
        {
            this.consumerInterceptor = consumerInterceptor;
        }

        protected override void AttachToComponentRegistration(IComponentRegistry componentRegistry, IComponentRegistration registration)
        {
            registration.Activating += (sender, e) =>
            {
                if (typeof (IMessageConsumer).IsAssignableFrom(e.Instance.GetType()))
                    consumerInterceptor.ItemCreated(e.Instance.GetType(), e.Component.Lifetime.GetType().Equals(typeof(CurrentScopeLifetime)));
            };
        }
    }
}