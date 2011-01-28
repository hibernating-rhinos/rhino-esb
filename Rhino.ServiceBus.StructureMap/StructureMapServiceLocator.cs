using System;
using System.Collections.Generic;
using System.Linq;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Internal;
using StructureMap;

namespace Rhino.ServiceBus.StructureMap
{
    public class StructureMapServiceLocator : IServiceLocator 
    {
        private readonly IContainer container;

        public StructureMapServiceLocator(IContainer container)
        {
            this.container = container;
        }

        public T Resolve<T>()
        {
            return container.GetInstance<T>();
        }

        public object Resolve(Type type)
        {
            return container.GetInstance(type);
        }

        public bool CanResolve(Type type)
        {
            return container.Model.HasDefaultImplementationFor(type);
        }

        public IEnumerable<T> ResolveAll<T>()
        {
            return container.GetAllInstances<T>();
        }

        public IEnumerable<IHandler> GetAllHandlersFor(Type type)
        {
            return from h in container.Model.AllInstances.Where(x => type.IsAssignableFrom(x.ConcreteType))
                   select (IHandler)
                       new DefaultHandler(h.PluginType, h.ConcreteType, () => container.GetInstance(h.ConcreteType));
        }

        public void Release(object item)
        {
            //Not needed for StructureMap it doesn't keep references beyond the life cycle that was configured.
        }
    }
}