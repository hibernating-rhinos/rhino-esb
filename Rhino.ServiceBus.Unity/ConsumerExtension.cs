using Microsoft.Practices.Unity;
using Rhino.ServiceBus.Config;
using Rhino.ServiceBus.Internal;

namespace Rhino.ServiceBus.Unity
{
    public class ConsumerExtension : UnityContainerExtension
    {
        private readonly IConsumerInterceptor interceptor;

        public ConsumerExtension(IConsumerInterceptor interceptor)
        {
            this.interceptor = interceptor;
        }

        protected override void Initialize()
        {
            Context.Registering += TypeRegistering;
        }

        private void TypeRegistering(object sender, RegisterEventArgs args)
        {
            if (typeof (IMessageConsumer).IsAssignableFrom(args.TypeTo) == false)
                return;

            args.LifetimeManager = new TransientLifetimeManager();
            interceptor.ItemCreated(args.TypeTo, true);
        }

        public override void Remove()
        {
            Context.Registering -= TypeRegistering;
        }
    }
}
