using System;
using System.Linq;
using System.Reflection;
using Microsoft.Practices.Unity;

namespace Rhino.ServiceBus.Unity
{
    public static class ContainerExtensions
    {
        public static void RegisterTypesFromAssembly(this IUnityContainer container, Assembly assemlyToScan, Type basedOn, params Type[] exclude)
        {
            assemlyToScan.GetTypes()
                .Where(t => basedOn.IsAssignableFrom(t) && !t.Equals(basedOn) && !t.IsInterface && !exclude.Contains(t)).ToList()
                .ForEach(type => container.RegisterType(basedOn, type, Guid.NewGuid().ToString(), new ContainerControlledLifetimeManager()));
        }

        public static void RegisterTypesFromAssembly<TBasedOn>(this IUnityContainer container, Assembly assemlyToScan, params Type[] exclude)
        {
            RegisterTypesFromAssembly(container, assemlyToScan, typeof(TBasedOn), exclude);
        }
    }
}