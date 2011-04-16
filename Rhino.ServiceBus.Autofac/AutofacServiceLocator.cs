using System;
using System.Collections.Generic;
using Autofac;
using Rhino.ServiceBus.Internal;

namespace Rhino.ServiceBus
{
    public class AutofacServiceLocator : IServiceLocator
    {
        private readonly IContainer container;

        public AutofacServiceLocator(IContainer container)
        {
            this.container = container;
        }

        public T Resolve<T>()
        {
            return container.Resolve<T>();
        }

        public object Resolve(Type type)
        {
            return container.Resolve(type);
        }

        public bool CanResolve(Type type)
        {
            return container.IsRegistered(type);
        }

        public IEnumerable<T> ResolveAll<T>()
        {
            return container.Resolve<IEnumerable<T>>();
        }

        public IEnumerable<IHandler> GetAllHandlersFor(Type type)
        {
            //TODO not sure what Autofac can do for this
            yield break;
        }

        public void Release(object item)
        {
            //Not needed for autofac
        }
    }
}