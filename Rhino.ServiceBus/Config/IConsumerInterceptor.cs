using System;

namespace Rhino.ServiceBus.Config
{
    public interface IConsumerInterceptor
    {
        void ItemCreated(Type createdItem, bool isTransient);
    }
}