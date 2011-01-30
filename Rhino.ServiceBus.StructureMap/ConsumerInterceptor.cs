using System;
using System.Linq;
using Rhino.ServiceBus.Exceptions;
using Rhino.ServiceBus.Sagas;
using StructureMap;
using StructureMap.Interceptors;

namespace Rhino.ServiceBus.StructureMap
{
    [CLSCompliant(false)]
    public  class ConsumerInterceptor : InstanceInterceptor
    {
        public object Process(object target, IContext context)
        {
            var consumerType = target.GetType();
            var interfaces = consumerType.GetInterfaces()
                .Where(x => x.IsGenericType && x.IsGenericTypeDefinition == false)
                .Select(x => x.GetGenericTypeDefinition())
                .ToList();

            if (interfaces.Contains(typeof(InitiatedBy<>)) &&
                interfaces.Contains(typeof(ISaga<>)) == false)
            {
                throw new InvalidUsageException("Message consumer: " + consumerType + " implements InitiatedBy<TMsg> but doesn't implment ISaga<TState>. " + Environment.NewLine +
                                                "Did you forget to inherit from ISaga<TState> ?");
            }

            if (interfaces.Contains(typeof(InitiatedBy<>)) == false &&
                interfaces.Contains(typeof(Orchestrates<>)))
            {
                throw new InvalidUsageException("Message consumer: " + consumerType + " implements Orchestrates<TMsg> but doesn't implment InitiatedBy<TState>. " + Environment.NewLine +
                                                "Did you forget to inherit from InitiatedBy<TState> ?");
            }
            return target;
        }
    }
}