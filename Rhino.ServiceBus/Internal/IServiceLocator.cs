using System;
using System.Collections.Generic;

namespace Rhino.ServiceBus.Internal
{
    public interface IServiceLocator
    {
        T Resolve<T>();
        object Resolve(Type type);
        bool CanResolve(Type type);
        IEnumerable<T> ResolveAll<T>();
        IEnumerable<IHandler> GetAllHandlersFor(Type type);
        void Release(object item);
    }
}