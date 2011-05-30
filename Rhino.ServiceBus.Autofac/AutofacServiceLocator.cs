using System;
using System.Collections.Generic;
using Autofac;
using Autofac.Core;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Internal;
using System.Linq;

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
        var services = container.ComponentRegistry.Registrations
          .SelectMany(r => r.Services.OfType<TypedService>())
          .Where(pService => type.IsAssignableFrom(pService.ServiceType));

        return services.Select(service => (IHandler)new DefaultHandler(type, service.ServiceType, () => container.ResolveService(service)));
      }

        public void Release(object item)
        {
            //Not needed for autofac
        }
    }
}