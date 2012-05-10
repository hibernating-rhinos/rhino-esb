using System;
using Rhino.ServiceBus.Config;
using Rhino.ServiceBus.Internal;
using StructureMap;
using StructureMap.Interceptors;

namespace Rhino.ServiceBus.StructureMap
{
    [CLSCompliant(false)]
    public  class ConsumerInterceptor : TypeInterceptor
    {
        private readonly IConsumerInterceptor interceptor;
        private readonly IContainer container;

        public ConsumerInterceptor(IConsumerInterceptor interceptor, IContainer container)
        {
            this.interceptor = interceptor;
            this.container = container;
        }

        public object Process(object target, IContext context)
        {
            var type = target.GetType();
            var lifecycle = container.Model.For(type).Lifecycle;
            interceptor.ItemCreated(type, string.IsNullOrEmpty(lifecycle) || lifecycle == "Transient"); //got to be a better way for this
            return target;
        }

        public bool MatchesType(Type type)
        {
            return typeof (IMessageConsumer).IsAssignableFrom(type);
        }
    }
}