using System;
using System.Collections.Generic;
using System.Linq;
using Castle.MicroKernel.Context;
using Castle.Windsor;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Internal;

namespace Rhino.ServiceBus.Castle
{
    public class CastleServiceLocator : IServiceLocator
    {
        private readonly IWindsorContainer container;

        public CastleServiceLocator(IWindsorContainer container)
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
            return container.Kernel.HasComponent(type);
        }

        public IEnumerable<T> ResolveAll<T>()
        {
            return container.ResolveAll<T>();
        }

        public IEnumerable<IHandler> GetAllHandlersFor(Type type)
        {
            return (from h in container.Kernel.GetAssignableHandlers(type)
                   select (IHandler)new DefaultHandler(h.Service, h.ComponentModel.Implementation, () => h.Resolve(CreationContext.Empty)));
        }

        public void Release(object item)
        {
            container.Kernel.ReleaseComponent(item);
        }
    }
}