using System;

using Rhino.ServiceBus.Config;
using Rhino.ServiceBus.Internal;

using Spring.Context;
using Spring.Objects.Factory.Config;

namespace Rhino.ServiceBus.Spring
{
    [CLSCompliant(false)]
    public  class ConsumerInterceptor : IObjectPostProcessor
    {
        private readonly IConsumerInterceptor interceptor;
        private readonly IConfigurableApplicationContext applicationContext;

        public ConsumerInterceptor(IConsumerInterceptor interceptor, IConfigurableApplicationContext applicationContext)
        {
            this.interceptor = interceptor;
            this.applicationContext = applicationContext;
        }

        public object PostProcessBeforeInitialization(object instance, string name)
        {
            return instance;
        }

        public object PostProcessAfterInitialization(object instance, string objectName)
        {
            var type = instance.GetType();
            if (typeof(IMessageConsumer).IsAssignableFrom(type))
            {
                var transient = !applicationContext.ObjectFactory.GetObjectDefinition(objectName).IsSingleton;
                interceptor.ItemCreated(type, transient);
            }
            return instance;
        }
    }
}