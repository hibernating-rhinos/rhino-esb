using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Practices.Unity;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Internal;

namespace Rhino.ServiceBus.Unity
{
    public class UnityServiceLocator : IServiceLocator
    {
        private readonly IUnityContainer container;

        public UnityServiceLocator(IUnityContainer container)
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
            return container.ResolveAll<T>();
        }

        public IEnumerable<IHandler> GetAllHandlersFor(Type type)
        {
            return container.Registrations
                    .Where(r => type.IsAssignableFrom(r.MappedToType))
                    .Select(r => (IHandler)new DefaultHandler(r.RegisteredType, r.MappedToType, () => container.Resolve(r.MappedToType)));
        }

        public void Release(object item)
        {
            //Not needed for Unity it doesn't keep references beyond the life cycle that was configured.
        }
    }
}
