using System;
using System.Linq;
using Rhino.ServiceBus.Exceptions;
using Rhino.ServiceBus.Internal;
using Rhino.ServiceBus.Sagas;

namespace Rhino.ServiceBus.Config
{
    public class ConsumerInterceptor : IConsumerInterceptor
    {
        public void ItemCreated(Type createdType, bool isTransient)
        {
            if (typeof(IMessageConsumer).IsAssignableFrom(createdType) == false)
                return;

            var interfaces = createdType.GetInterfaces()
                .Where(x => x.IsGenericType && x.IsGenericTypeDefinition == false)
                .Select(x => x.GetGenericTypeDefinition())
                .ToList();

            if (interfaces.Contains(typeof(InitiatedBy<>)) &&
                interfaces.Contains(typeof(ISaga<>)) == false)
            {
                throw new InvalidUsageException("Message consumer: " + createdType + " implements InitiatedBy<TMsg> but doesn't implment ISaga<TState>. " + Environment.NewLine +
                                                "Did you forget to inherit from ISaga<TState> ?");
            }

            if (interfaces.Contains(typeof(InitiatedBy<>)) == false &&
                interfaces.Contains(typeof(Orchestrates<>)))
            {
                throw new InvalidUsageException("Message consumer: " + createdType + " implements Orchestrates<TMsg> but doesn't implment InitiatedBy<TState>. " + Environment.NewLine +
                                                "Did you forget to inherit from InitiatedBy<TState> ?");
            }

            if(isTransient == false)
                throw new InvalidUsageException("Consumers are required to be transient.");
        }
    }
}