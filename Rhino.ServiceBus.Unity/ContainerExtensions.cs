using System;
using System.Linq;
using System.Reflection;
using Microsoft.Practices.Unity;

namespace Rhino.ServiceBus.Unity
{
    public static class ContainerExtensions
    {
        public static void RegisterTypesFromAssembly(this IUnityContainer container, Assembly assemlyToScan, Type basedOn, Predicate<Type> condition)
        {
            assemlyToScan.GetTypes()
                .Where(t => condition(t)).ToList()
                .ForEach(type => container.RegisterType(basedOn, type, Guid.NewGuid().ToString(), new ContainerControlledLifetimeManager()));
        }

        public static void RegisterTypesFromAssembly<TBasedOn>(this IUnityContainer container, Assembly assemlyToScan, params Type[] excludes)
        {
            RegisterTypesFromAssembly<TBasedOn>(container, assemlyToScan, (Predicate<Type>)(x => !x.IsAbstract && !x.IsInterface && typeof(TBasedOn).IsAssignableFrom(x) && !excludes.Contains(x)));
        }

        public static void RegisterTypesFromAssembly<TBasedOn>(this IUnityContainer container, Assembly assemlyToScan, Predicate<Type> condition)
        {
            RegisterTypesFromAssembly(container, assemlyToScan, typeof(TBasedOn), condition);
        }
    }
}